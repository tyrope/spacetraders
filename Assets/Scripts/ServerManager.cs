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

        private static readonly string Server = "https://api.spacetraders.io/v2/";
        private static readonly Queue<DateTime> LastCalls = new Queue<DateTime>();

        public async static Task<(ServerResult, T)> CachedRequest<T>( string endpoint, TimeSpan lifespan, RequestMethod method, CancellationTokenSource cancel, string payload = null ) {
            // Grab data from cache.
            (CacheHandler.ReturnCode code, string cacheData) = CacheHandler.Load(endpoint);
            if(code == CacheHandler.ReturnCode.SUCCESS) {
                // Success!
                Debug.Log($"[Cache]{endpoint}\n<= {cacheData}");
                return (new ServerResult(ServerResult.ResultType.SUCCESS, "Loaded from cache"), JsonConvert.DeserializeObject<T>(cacheData));
            }
            // Or grab it from the API instead.
            (ServerResult res, T result) = await Request<T>(endpoint, method, cancel, payload);

            // Save it to the Cache if successful.
            if(res.result == ServerResult.ResultType.SUCCESS) {
                CacheHandler.Save(endpoint, JsonConvert.SerializeObject(result), lifespan);
            }
            return (res, result); // Done!
        }
        public async static Task<(ServerResult, T)> Request<T>( string endpoint, RequestMethod method, CancellationTokenSource cancel, string payload = null, string authToken = null ) {
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
            }else if(PlayerPrefs.HasKey("AuthToken")) { 
                request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken"));
            }

            // Rate limiting.
            while(LastCalls.Count >= 10) {
                // If we're caught in this block, we've hit the rate limiter.
                while(LastCalls.Count > 0 && DateTime.Compare(
                        LastCalls.Peek(),
                        DateTime.Now - new TimeSpan(0, 0, 0, 0, 500)
                ) < 0) {
                    // Expire any tokens older than 500ms
                    LastCalls.Dequeue();
                }

                // Check if we're allowed to break out of the buffer yet.
                if(LastCalls.Count < 10) { break; }

                // Wait a frame.
                await Task.Yield();
            }
            LastCalls.Enqueue(DateTime.Now);
            request.SendWebRequest();

            while(request.result == UnityWebRequest.Result.InProgress) { await Task.Yield(); }

            string err;
            switch(request.result) {
                case UnityWebRequest.Result.ProtocolError:
                    Log(method, endpoint, LastCalls.Count, $"HTTPError: { request.error}\n{ request.downloadHandler.text}", payload: payload);
                    err = request.error;
                    request.Dispose();
                    return (new ServerResult(ServerResult.ResultType.HTTP_ERROR, err), default);
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Log(method, endpoint, LastCalls.Count, $"Error: { request.error}\n{ request.downloadHandler.text}", payload: payload);
                    err = request.error;
                    request.Dispose();
                    return (new ServerResult(ServerResult.ResultType.PROCESSING_ERROR, err), default);
                case UnityWebRequest.Result.Success:
                    string retstring = request.downloadHandler.text;
                    request.Dispose();
                    try {
                        // Unwrap a potential ServerResponse.
                        ServerResponse<T> resp = JsonConvert.DeserializeObject<ServerResponse<T>>(retstring);
                        Log(method, endpoint, LastCalls.Count, retstring, resp.meta?.ToString(), payload);
                        return (new ServerResult(ServerResult.ResultType.SUCCESS), resp.data);
                    } catch(JsonSerializationException) {
                        // There was no ServerResponse wrapper.
                        Log(method, endpoint, LastCalls.Count, retstring, payload:payload);
                        return (new ServerResult(ServerResult.ResultType.SUCCESS), JsonConvert.DeserializeObject<T>(retstring));
                    }
                default:
                    Debug.LogError("Theoretically unreachable code found!");
                    request.Dispose();
                    return (new ServerResult(ServerResult.ResultType.UNKNOWN_ERROR, "Unreachable code."), default);
            }
        }

        private static void Log( RequestMethod method, string endpoint, int ratelimit, string response, string meta = null, string payload = null ) {
            string logString = $"[API:{method}]{endpoint} - Rate Limit:{ratelimit}/10\n";
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
    }
}
