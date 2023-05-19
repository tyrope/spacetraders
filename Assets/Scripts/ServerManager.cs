using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SpaceTraders
{

    public enum RequestMethod { GET, POST }
    public class ServerManager
    {
        private class ServerResponse<T>
        {
            public T data;
            public Meta meta;
        }

        private readonly static string Server = "https://api.spacetraders.io/v2/";

        private static readonly Queue<DateTime> LastCalls = new Queue<DateTime>();

        public async static Task<(bool, T)> CachedRequest<T>( string endpoint, TimeSpan lifespan, RequestMethod method, CancellationTokenSource cancel, string payload = null ) {
            // Grab data from cache.
            (CacheHandler.ReturnCode code, string cacheData) = CacheHandler.Load(endpoint);
            if(code == CacheHandler.ReturnCode.SUCCESS){
                // Success!
                Debug.Log($"[Cache]{endpoint}\n{cacheData}");
                return (true, JsonConvert.DeserializeObject<T>(cacheData));
            }

            // Grab it from the API instead.
            (bool success, T result) = await Request<T>(endpoint, method, cancel, payload);

            if(success) {
                // Save it to the Cache. (This might error. Oh well.)
                CacheHandler.Save(endpoint, JsonConvert.SerializeObject(result), lifespan);
            }

            // Done!
            return (success, result);
        }

        public async static Task<(bool, T)> Request<T>( string endpoint, RequestMethod method, CancellationTokenSource cancel, string payload = null, string authToken = null) {
            string uri = Server + endpoint;

            UnityWebRequest request;
            switch(method) {
                case RequestMethod.GET:
                    request = UnityWebRequest.Get(new System.Uri(uri));
                    break;
                case RequestMethod.POST:
                    request = UnityWebRequest.Post(new System.Uri(uri), payload);
                    break;
                default:
                    throw new System.NotImplementedException("The method you've requested is not yet available.");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if(authToken == null) {
                request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken"));
            } else {
                // Override for AuthCheck to use.
                request.SetRequestHeader("Authorization", "Bearer " + authToken);
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
                if(LastCalls.Count < 10) {
                    break;
                }

                // Wait a frame.
                await Task.Yield();
            }

            request.SendWebRequest();
            LastCalls.Enqueue(DateTime.Now);

            while(request.result == UnityWebRequest.Result.InProgress) {
                await Task.Yield();
            }

            switch(request.result) {
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError($"[API:{method}]{endpoint} => Rate limit: {LastCalls.Count}/10\nHTTPError: {request.error}\n{request.downloadHandler.text}");
                    request.Dispose();
                    return (false, default);
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError($"[API:{method}]{endpoint} => Rate limit: {LastCalls.Count}/10\nError: {request.error}\n{request.downloadHandler.text}");
                    request.Dispose();
                    return (false, default);
                case UnityWebRequest.Result.Success:
                    string retstring = request.downloadHandler.text;
                    request.Dispose();
                    try {
                        // Unwrap a potential ServerResponse.
                        ServerResponse<T> resp = JsonConvert.DeserializeObject<ServerResponse<T>>(retstring);
                        if(resp.meta != null) {
                            Debug.Log($"[API:{method}]{endpoint} => Rate limit: {LastCalls.Count}/10\nShowing {resp.meta}\n{retstring}");
                        } else {
                            Debug.Log($"[API:{method}]{endpoint} => Rate limit: {LastCalls.Count}/10\n{retstring}");
                        }
                        return (true, resp.data);
                    } catch(JsonSerializationException) {
                        // There was no ServerResponse wrapper.
                        Debug.Log($"[API:{method}]{endpoint} => Rate limit: {LastCalls.Count}/10\n{retstring}");
                        return (true, JsonConvert.DeserializeObject<T>(retstring));
                    }
                default:
                    Debug.LogError("Theoretically unreachable code found!");
                    request.Dispose();
                    return (false, default);
            }
        }
    }
}
