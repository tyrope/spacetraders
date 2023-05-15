using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SpaceTraders
{
    public class ServerManager
    {
        private readonly static string Server = "https://api.spacetraders.io/v2/";

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

        public static T Get<T>( string endpoint, string AuthToken = null ) {
            string uri = Server + endpoint;

            using UnityWebRequest request = UnityWebRequest.Get(new System.Uri(uri));
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
                    return default;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + request.error);
                    return default;
                case UnityWebRequest.Result.Success:
                    return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                default:
                    Debug.LogError("Theoretically unreachable code found!");
                    return default;
            }
        }
    }
}
