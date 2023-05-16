using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SpaceTraders
{
    public enum RequestMethod { GET, POST }
    public class ServerManager
    {
        private readonly static string Server = "https://api.spacetraders.io/v2/";

        public static T Request<T>( RequestMethod method, string endpoint, string payload = null, string AuthToken = null ) {
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
