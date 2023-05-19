using System.Threading;
using UnityEngine;

namespace STCommander
{
    public class ConsoleController : MonoBehaviour
    {
        public GameObject console;
        public TMPro.TMP_InputField endpoint;
        public TMPro.TMP_InputField payload;
        public TMPro.TMP_Dropdown method;

        private readonly CancellationTokenSource asyncCancelToken = new CancellationTokenSource();

        /// <summary>
        /// Gives this controller a chance to interprete inputs.
        /// </summary>
        /// <returns>true if future controllers are allowed to parse inputs as well.</returns>
        public bool ParseInputs() {
            if(Input.GetKeyDown(KeyCode.Return)) {
                console.SetActive(console.activeSelf == false);
            }
            Cursor.lockState = console.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;

            return console.activeSelf == false;
        }

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
                    Debug.LogError("ConsoleController::CallAPI() - Unknown method:" + method.value);
                    break;
            }
        }
        #pragma warning restore CS4014
    }
}
