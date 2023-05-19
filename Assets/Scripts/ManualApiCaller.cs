using System.Threading;
using UnityEngine;

namespace STCommander
{
    public class ManualApiCaller : MonoBehaviour
    {
        public TMPro.TMP_InputField endpoint;
        public TMPro.TMP_InputField payload;
        public TMPro.TMP_Dropdown method;

        private readonly CancellationTokenSource asyncCancelToken = new CancellationTokenSource();

        #pragma warning disable CS4014 // Don't warn me about not awaiting async stuff. Manual API calls are expected to block UI.
        public void CallAPI() {
            switch(method.value) {
                case 0:
                    ServerManager.CachedRequest<object>(endpoint.text.Trim(), System.TimeSpan.Zero, RequestMethod.GET, asyncCancelToken, payload.text.Trim());
                    break;
                case 1:
                    ServerManager.CachedRequest<object>(endpoint.text.Trim(), System.TimeSpan.Zero, RequestMethod.POST, asyncCancelToken, payload.text.Trim());
                    break;
                default:
                    Debug.LogError("ManualApiCaller::CallAPI() - Unknown method:" + method.value);
                    break;
            }
        }
        #pragma warning restore CS4014
    }
}
