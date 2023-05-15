using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SpaceTraders
{
    public enum REQUEST_METHOD { GET, POST }

    public class ServerResponse<T>
    {
        public List<T> data;
        public Meta meta;

        public override string ToString() {
            return $"Server Response with {data.Count}/{meta.total} results. (Limit: {meta.limit}, page {meta.page}/{meta.TotalPages}";
        }
    }

    public class Meta
    {
        public int total;
        public int page;
        public int limit;
        public int TotalPages => Mathf.CeilToInt(total / limit);
    }

    public class ServerManager
    {
        private readonly static string Server = "https://api.spacetraders.io/v2/";

        public static T Request<T>( REQUEST_METHOD method, string endpoint, string payload = null, string AuthToken = null ) {
            string uri = Server + endpoint;

            UnityWebRequest request;
            if(method == REQUEST_METHOD.GET) {
                request = UnityWebRequest.Get(new System.Uri(uri));
            } else if (method == REQUEST_METHOD.POST) {
                request = UnityWebRequest.Post(new System.Uri(uri), payload);
            } else {
                throw new System.NotImplementedException("The method you've requested is not yet available.");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if(AuthToken == null) {
                request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken"));
            } else {
                // Override for AuthCheck to use.
                request.SetRequestHeader("Authorization", "Bearer " + AuthToken);
            }
            request.SendWebRequest();

            while(request.result == UnityWebRequest.Result.InProgress); // Thread-blocking!

            switch(request.result) {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Error: " + request.error);
                    request.Dispose();
                    return default;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + request.error);
                    request.Dispose();
                    return default;
                case UnityWebRequest.Result.Success:
                    string ret = request.downloadHandler.text;
                    request.Dispose();
                    return JsonConvert.DeserializeObject<T>(ret);
                default:
                    Debug.LogError("Theoretically unreachable code found!");
                    request.Dispose();
                    return default;
            }
        }
    }
}
