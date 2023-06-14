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

        private readonly CancellationTokenSource AsyncCancel = new CancellationTokenSource();

        private void OnDestroy() {
            AsyncCancel.Cancel();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        /// <summary>
        /// Gives this controller a chance to interprete inputs.
        /// </summary>
        /// <returns>true if future controllers are allowed to parse inputs as well.</returns>
        public bool ParseInputs() {
            if(Input.GetKeyDown(KeyCode.Return)) { console.SetActive(console.activeSelf == false); }
            Cursor.lockState = console.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
            return console.activeSelf == false;
        }

        public async void CallAPI() {
            switch(method.value) {
                case 0:
                    await ServerManager.RequestByPassCache<object>(endpoint.text.Trim(),RequestMethod.GET, AsyncCancel.Token, payload.text.Trim());
                    break;
                case 1:
                    await ServerManager.RequestByPassCache<object>(endpoint.text.Trim(), RequestMethod.POST, AsyncCancel.Token, payload.text.Trim());
                    break;
                default:
                    Debug.LogError("ConsoleController::CallAPI() - Unknown method:" + method.value);
                    break;
            }
        }
    }
}