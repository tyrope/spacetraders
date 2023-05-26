using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private class ServerResponse<T>
        {
            public T data;
            public Meta meta;
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
            public RateLimit(int poolSize, TimeSpan reset) {
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
                }else if(Amount < limit) {
                    Amount++;
                    return true;
                }
                return false;
            }
        }

        private static readonly string Server = "https://api.spacetraders.io/v2/";
        private static readonly RateLimit TrickleLimit = new RateLimit(2, new TimeSpan(0, 0, 1));
        private static readonly RateLimit BurstLimit = new RateLimit(10, new TimeSpan(0, 0, 10));

        
        private enum LogVerbosity { NONE, ERROR_ONLY, API_ONLY, EVERYTHING } //TODO Log Verbosity switch lives here.
        private static readonly LogVerbosity sendResultsToLog = LogVerbosity.ERROR_ONLY;

        public async static Task<(ServerResult, T)> CachedRequest<T>( string endpoint, TimeSpan lifespan, RequestMethod method, CancellationTokenSource cancel, string payload = null ) {
            // Remove any starting or trailing slashes.
            endpoint = endpoint.Trim('/');

            // Grab data from cache.
            (CacheManager.ReturnCode code, string cacheData) = CacheManager.Load(endpoint);
            if(code == CacheManager.ReturnCode.SUCCESS) {
                // Success!
                if(sendResultsToLog == LogVerbosity.EVERYTHING) {
                    Debug.Log($"[Cache]{endpoint}\n<= {cacheData}");
                }
                return (new ServerResult(ServerResult.ResultType.SUCCESS, "Loaded from cache"), JsonConvert.DeserializeObject<T>(cacheData));
            }
            // Or grab it from the API instead.
            (ServerResult res, T result) = await Request<T>(endpoint, method, cancel, payload);

            // Save it to the Cache if successful.
            if(res.result == ServerResult.ResultType.SUCCESS) {
                CacheManager.Save(endpoint, JsonConvert.SerializeObject(result), lifespan);
            }
            return (res, result); // Done!
        }
        public async static Task<(ServerResult, T)> Request<T>( string endpoint, RequestMethod method, CancellationTokenSource cancel, string payload = null, string authToken = null ) {
            // Remove any starting or trailing slashes.
            endpoint = endpoint.Trim('/');
            string uri = Server + endpoint;

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
                // Override for AuthCheck to use.
                request.SetRequestHeader("Authorization", "Bearer " + authToken);
            } else if(PlayerPrefs.HasKey("AuthToken")) {
                request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken"));
            }

            // Rate limiting.
            while(true) {
                if(TrickleLimit.TryMakeRequest()) {
                    break;
                } else if(BurstLimit.TryMakeRequest()) {
                    break;
                }
                TimeSpan delay = (TrickleLimit.ResetTime < BurstLimit.ResetTime ? TrickleLimit.ResetTime : BurstLimit.ResetTime) - DateTime.Now;
                await Task.Delay((int) Math.Ceiling(delay.TotalMilliseconds));
            }
            request.SendWebRequest();

            while(request.result == UnityWebRequest.Result.InProgress) {
                await Task.Yield();
                if(cancel.IsCancellationRequested) {
                    request.Abort();
                    request.Dispose();
                    return default;
                }
            }

            string err;
            switch(request.result) {
                case UnityWebRequest.Result.ProtocolError:
                    if(sendResultsToLog != LogVerbosity.NONE)
                        Log(method, endpoint, $"HTTPError: { request.error}\n{ request.downloadHandler.text}", payload: payload);
                    err = request.error;
                    request.Dispose();
                    return (new ServerResult(ServerResult.ResultType.HTTP_ERROR, err), default);
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    if(sendResultsToLog != LogVerbosity.NONE)
                        Log(method, endpoint, $"Error: { request.error}\n{ request.downloadHandler.text}", payload: payload);
                    err = request.error;
                    request.Dispose();
                    return (new ServerResult(ServerResult.ResultType.PROCESSING_ERROR, err), default);
                case UnityWebRequest.Result.Success:
                    string retstring = request.downloadHandler.text;
                    request.Dispose();
                    try {
                        // Unwrap a potential ServerResponse.
                        ServerResponse<T> resp = JsonConvert.DeserializeObject<ServerResponse<T>>(retstring);
                        if(sendResultsToLog != LogVerbosity.NONE && sendResultsToLog != LogVerbosity.ERROR_ONLY)
                            Log(method, endpoint, retstring, resp.meta?.ToString(), payload);
                        return (new ServerResult(ServerResult.ResultType.SUCCESS), resp.data);
                    } catch(JsonSerializationException) {
                        // There was no ServerResponse wrapper.
                        if(sendResultsToLog != LogVerbosity.NONE && sendResultsToLog != LogVerbosity.ERROR_ONLY)
                            Log(method, endpoint, retstring, payload: payload);
                        return (new ServerResult(ServerResult.ResultType.SUCCESS), JsonConvert.DeserializeObject<T>(retstring));
                    }
                default:
                    Debug.LogError("Theoretically unreachable code found!");
                    request.Dispose();
                    return (new ServerResult(ServerResult.ResultType.UNKNOWN_ERROR, "Unreachable code."), default);
            }
        }

        private static void Log( RequestMethod method, string endpoint, string response, string meta = null, string payload = null ) {
            string logString = $"[API:{method}]{endpoint} - Rate limiters: {RateLimitStatus()}\n";
            if(payload != null) {
                logString += $"=> {payload}\n";
            }
            if(meta != null) {
                logString += $"<= Showing {meta}\n{response}";
            } else {
                logString += $"<= {response}";
            }
            Debug.Log(logString);
        }

        private static string RateLimitStatus() {
            string trickleTime = Mathf.Max(0, (float) (TrickleLimit.ResetTime - DateTime.Now).TotalSeconds).ToString("F2");
            string burstTime = Mathf.Max(0, (float) (BurstLimit.ResetTime - DateTime.Now).TotalSeconds).ToString("F2");
            return $"T{TrickleLimit.Amount}/{TrickleLimit.limit}({trickleTime}s) - B{BurstLimit.Amount}/{BurstLimit.limit}({burstTime}s)";
        }
    }
}