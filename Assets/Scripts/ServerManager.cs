using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace STCommander
{
    public enum RequestMethod { GET, POST }
    public class ServerResult
    {
        public enum ResultType { UNKNOWN_ERROR = -1, SUCCESS, HTTP_ERROR, PROCESSING_ERROR, API_ERROR }
        public ResultType result;
        public string details;

        public ServerResult( ResultType result, string details = null ) {
            this.result = result;
            this.details = details;
        }
        public override string ToString() => $"[{result}]{details}";
    }
    public class ServerManager
    {
        public class Meta
        {
            public int total;
            public int page;
            public int limit;
            public int TotalPages => Mathf.CeilToInt((float) total / (float) limit);

            public override string ToString() => $"{Mathf.Min(total, limit)} result{(total > 1 ? "s" : "")} (Page {page}/{TotalPages})";
        }
        private class ServerResponse<T>
        {
            public T data;
            public Meta meta;
        }

        private class ErrorResponse
        {
            public class Error
            {
                public string message;
                public int code;
            }
            public Error error;
        }

        private class RateLimit
        {
            public DateTime ResetTime { get; private set; }
            public int Amount { get; private set; } = 0;
            public readonly int limit;
            private readonly TimeSpan cooldown;

            /// <summary>
            /// Instantiate a new ratelimit pool.
            /// </summary>
            /// <param name="poolSize">The size of the pool.</param>
            /// <param name="reset">How long it takes for the pool to refill.</param>
            public RateLimit( int poolSize, TimeSpan reset ) {
                cooldown = reset;
                limit = poolSize;
                ResetTime = DateTime.Now;
            }

            /// <summary>
            /// Check the rate limiter to see if we're allowed to make a request right now.
            /// </summary>
            /// <returns>true if we the request is allowed through right now. false if we're being limited.</returns>
            public bool TryMakeRequest() {
                if(Amount == 0 || ResetTime < DateTime.Now) {
                    ResetTime = DateTime.Now + cooldown;
                    Amount = 1;
                    return true;
                } else if(Amount < limit) {
                    Amount++;
                    return true;
                }
                return false;
            }
        }

        private static readonly string Server = "https://api.spacetraders.io/v2/";
        private static readonly RateLimit TrickleLimit = new RateLimit(2, new TimeSpan(0, 0, 1));
        private static readonly RateLimit BurstLimit = new RateLimit(10, new TimeSpan(0, 0, 10));


        private enum LogVerbosity { NONE, ERROR_ONLY, API_ONLY, EVERYTHING } //TODO API Verbosity switch lives here.
        private static readonly LogVerbosity sendResultsToLog = LogVerbosity.ERROR_ONLY;

        public static async Task<(ServerResult, List<T>)> RequestList<T>( string endpoint, TimeSpan maxAge, RequestMethod method, CancellationToken cancel, string payload = null ) where T : IDataClass {
            // Remove any starting or trailing slashes.
            endpoint = endpoint.Trim('/');

            // Grab data from cache.
            List<IDataClass> cacheData = await ((T) Activator.CreateInstance(typeof(T))).LoadFromCache(endpoint, maxAge, cancel);
            if(cancel.IsCancellationRequested) { return default; }
            if(cacheData != null && cacheData.Count > 0) {
                // Success!
                if(sendResultsToLog == LogVerbosity.EVERYTHING) {
                    Debug.Log($"[Cache]{endpoint}\n<= {cacheData.Count}x {typeof(T)}: {string.Join(" -- ", cacheData)}");
                }
                return (new ServerResult(ServerResult.ResultType.SUCCESS, "Loaded from cache"), cacheData.Cast<T>().ToList());
            }
            // Or grab it from the API instead.
            (ServerResult res, T[] result) = await RequestByPassCache<T[]>(endpoint, method, cancel, payload);
            if(cancel.IsCancellationRequested) { return default; }

            // Save it to the Cache if successful.
            if(res.result == ServerResult.ResultType.SUCCESS) {
                foreach(T r in result) {
                    if(await r.SaveToCache(cancel) == false) {
                        if(sendResultsToLog >= LogVerbosity.ERROR_ONLY)
                            Debug.LogError("Failed to save to cache: " + r);
                    } else if(sendResultsToLog >= LogVerbosity.EVERYTHING) {
                        Debug.Log("Saved to cache: " + r);
                    }
                    if(cancel.IsCancellationRequested) { return default; }
                }
            }
            return (res, result?.ToList()); // Done!
        }
        public static async Task<(ServerResult, T)> RequestSingle<T>( string endpoint, TimeSpan maxAge, RequestMethod method, CancellationToken cancel, string payload = null ) where T : IDataClass {
            // Remove any starting or trailing slashes.
            endpoint = endpoint.Trim('/');

            // Grab data from cache.
            List<IDataClass> cacheData = await ((T) Activator.CreateInstance(typeof(T))).LoadFromCache(endpoint, maxAge, cancel);
            if(cancel.IsCancellationRequested) { return default; }
            if(cacheData != null) {
                // Success!
                if(cacheData.Count > 1) {
                    // Sort of.
                    Debug.LogWarning($"ServerManager::RequestSingle() -- Received {cacheData.Count} results, only returning the first.\n{endpoint}");
                }
                if(sendResultsToLog == LogVerbosity.EVERYTHING) {
                    Debug.Log($"[Cache]{endpoint}\n<= {cacheData[0]}");
                }
                return (new ServerResult(ServerResult.ResultType.SUCCESS, "Loaded from cache"), (T) cacheData[0]);
            }
            // Or grab it from the API instead.
            (ServerResult res, T result) = await RequestByPassCache<T>(endpoint, method, cancel, payload);
            if(cancel.IsCancellationRequested) { return default; }

            // Save it to the Cache if successful.
            if(res.result == ServerResult.ResultType.SUCCESS) {
                if(await result.SaveToCache(cancel) == false) {
                    if(sendResultsToLog >= LogVerbosity.ERROR_ONLY)
                        Debug.LogError("Failed to save to cache: " + result);
                } else if(sendResultsToLog >= LogVerbosity.EVERYTHING) {
                    Debug.Log("Saved to cache: " + result);
                }
                if(cancel.IsCancellationRequested) { return default; }
            }
            return (res, result); // Done!
        }
        public async static Task<(ServerResult, T)> RequestByPassCache<T>( string endpoint, RequestMethod method, CancellationToken cancel, string payload = null, string authToken = null ) {
            // Remove any starting or trailing slashes.
            endpoint = endpoint.Trim('/');
            string uri = Server + endpoint;

            await WaitForRateLimiter();

            using UnityWebRequest request = MakeRequest(method, uri, payload, authToken);
            request.SendWebRequest();
            while(request.result == UnityWebRequest.Result.InProgress) {
                await Task.Yield();
                if(cancel.IsCancellationRequested) {
                    request.Abort();
                    return default;
                }
            }

            switch(request.result) {
                case UnityWebRequest.Result.ProtocolError: //HTTP Errors
                    //Error handling:
                    ErrorResponse.Error error = JsonConvert.DeserializeObject<ErrorResponse>(request.downloadHandler.text).error;
                    switch(error.code) {
                        case 409: // Conflict
                            int delay = UnityEngine.Random.Range(10, 1001);
                            if(sendResultsToLog >= LogVerbosity.ERROR_ONLY)
                                Log(method, endpoint, $"HTTPError: {request.error}\nTrying again in {delay}ms.", payload: payload);
                            await Task.Delay(delay);
                            return await RequestByPassCache<T>(endpoint, method, cancel, payload, authToken);
                        case 429: // Too Many Requests
                            if(sendResultsToLog >= LogVerbosity.ERROR_ONLY)
                                Log(method, endpoint, $"HTTPError: { request.error }\nTrying again in 1s.", payload: payload);
                            await Task.Delay(1000);
                            return await RequestByPassCache<T>(endpoint, method, cancel, payload, authToken);
                        default:
                            if(sendResultsToLog >= LogVerbosity.ERROR_ONLY)
                                Log(method, endpoint, $"HTTPError: { request.error}\n{ request.downloadHandler.text}", payload: payload, error: true);
                            break;
                    }
                    return (new ServerResult(ServerResult.ResultType.HTTP_ERROR, request.error), default);
                case UnityWebRequest.Result.ConnectionError: // Connection Errors.
                case UnityWebRequest.Result.DataProcessingError: // API Errors.
                    if(sendResultsToLog >= LogVerbosity.ERROR_ONLY)
                        Log(method, endpoint, $"Error: { request.error}\n{ request.downloadHandler.text}", payload: payload, error: true);
                    return (new ServerResult(ServerResult.ResultType.PROCESSING_ERROR, request.error), default);
                case UnityWebRequest.Result.Success: // Success!
                    string retstring = request.downloadHandler.text;
                    try {
                        // Unwrap a potential ServerResponse.
                        ServerResponse<T> resp = JsonConvert.DeserializeObject<ServerResponse<T>>(retstring);
                        if(sendResultsToLog >= LogVerbosity.API_ONLY)
                            Log(method, endpoint, retstring, resp.meta?.ToString(), payload);
                        return (new ServerResult(ServerResult.ResultType.SUCCESS), resp.data);
                    } catch(JsonSerializationException) {
                        // There was no ServerResponse wrapper.
                        if(sendResultsToLog >= LogVerbosity.API_ONLY)
                            Log(method, endpoint, retstring, payload: payload);
                        try {
                            return (new ServerResult(ServerResult.ResultType.SUCCESS), JsonConvert.DeserializeObject<T>(retstring));
                        } catch(JsonReaderException) {
                            // This breaks and idk why.
                            if(sendResultsToLog >= LogVerbosity.ERROR_ONLY)
                                Log(method, endpoint, "JsonReaderException trying to parse:\n" + retstring, payload: payload, error: true);
                            return (new ServerResult(ServerResult.ResultType.PROCESSING_ERROR, "Unparsable JSON"), default);
                        }
                    }
                default:
                    Debug.LogError("Theoretically unreachable code found!");
                    return (new ServerResult(ServerResult.ResultType.UNKNOWN_ERROR, "Unreachable code."), default);
            }
        }

        private static async Task WaitForRateLimiter() {
            while(true) {
                if(TrickleLimit.TryMakeRequest()) {
                    break;
                } else if(BurstLimit.TryMakeRequest()) {
                    break;
                }
                TimeSpan delay = (TrickleLimit.ResetTime < BurstLimit.ResetTime ? TrickleLimit.ResetTime : BurstLimit.ResetTime) - DateTime.Now;
                await Task.Delay((int) Math.Ceiling(delay.TotalMilliseconds));
            }
        }

        /// <summary>
        /// This function instantiates a UnityWebRequest object, but does NOT send the request out.
        /// </summary>
        /// <param name="method">Method of contacting the server (GET, POST, etc)</param>
        /// <param name="uri">The full link to contact</param>
        /// <param name="payload">The JSON payload to send for POST requests.</param>
        /// <param name="authToken">The authentication token to send, if left null we'll try the token stored in PlayerPrefs.</param>
        /// <returns>An *unconnected* UnityWebRequest.</returns>
        private static UnityWebRequest MakeRequest( RequestMethod method, string uri, string payload, string authToken ) {
            UnityWebRequest request;
            switch(method) {
                case RequestMethod.GET:
                    request = UnityWebRequest.Get(new Uri(uri));
                    break;
                case RequestMethod.POST:
                    byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
                    request = new UnityWebRequest(uri, "POST") {
                        uploadHandler = new UploadHandlerRaw(payloadBytes)
                    };
                    break;
                default:
                    throw new NotImplementedException("The method you've requested is not yet available.");
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if(authToken != null) {
                request.SetRequestHeader("Authorization", "Bearer " + authToken);
            } else if(PlayerPrefs.HasKey("AuthToken")) {
                request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken"));
            }
            return request;
        }

        /// <summary>
        /// Wrapper function for logging to the Unity console.
        /// </summary>
        /// <param name="error">do we use Debug.LogError instead of Debug.Log?</param>
        private static void Log( RequestMethod method, string endpoint, string response, string meta = null, string payload = null, bool error = false ) {
            string logString = $"[API:{method}]{endpoint} - Rate limiters: {RateLimitStatus()}\n";
            if(payload != null) {
                logString += $"=> {payload}\n";
            }
            if(meta != null) {
                logString += $"<= Showing {meta}\n{response}";
            } else {
                logString += $"<= {response}";
            }
            if(error) {
                Debug.LogError(logString);
            } else {
                Debug.Log(logString);
            }
        }

        private static string RateLimitStatus() {
            string trickleTime = Mathf.Max(0, (float) (TrickleLimit.ResetTime - DateTime.Now).TotalSeconds).ToString("F2");
            string burstTime = Mathf.Max(0, (float) (BurstLimit.ResetTime - DateTime.Now).TotalSeconds).ToString("F2");
            return $"T{TrickleLimit.Amount}/{TrickleLimit.limit}({trickleTime}s) - B{BurstLimit.Amount}/{BurstLimit.limit}({burstTime}s)";
        }
    }
}