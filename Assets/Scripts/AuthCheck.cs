using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace STCommander
{
    public class AuthCheck : MonoBehaviour {
        public class RegistrationResult {
            public class Data {
                public AgentInfo agent;
                public Contract contract;
                public Faction faction;
                public Ship ship;
                public string token;
            }
            public Data data;
        }

        public TMPro.TMP_InputField NameInputField;
        public TMPro.TMP_Dropdown FactionDropdown;
        public TMPro.TMP_InputField EmailInputField;
        public TMPro.TMP_Text RegisterButtonText;

        public GameObject SaveWarning;
        public TMPro.TMP_InputField TokenInputField;
        public TMPro.TMP_Text LoginButtonText;

        public bool RevokeAuthorization = false;
        private float registerErrorTime = 0f;
        private float loginErrorTime = 0f;
        private readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        // Start is called before the first frame update
        void Start() {
            // We're authorized already.
            if(RevokeAuthorization) {
                PlayerPrefs.DeleteKey("AuthToken");
                PlayerPrefs.Save();
            } else if(PlayerPrefs.HasKey("AuthToken")) {
                CheckAuthorization(true);
                return;
            }
        }

        void Update() {
            if(registerErrorTime > 0f) {
                registerErrorTime -= Time.deltaTime;
            }
            RegisterButtonText.color = Color.Lerp(Color.white, Color.red, registerErrorTime / 2f);

            if(loginErrorTime > 0f) {
                loginErrorTime -= Time.deltaTime;
            }
            LoginButtonText.color = Color.Lerp(Color.white, Color.red, loginErrorTime / 2f);
        }

        private void OnDestroy() {
            AsyncCancelToken.Cancel();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        public async void CreateAgent() {
            string req;
            string faction = FactionDropdown.options[FactionDropdown.value].text.Trim();
            string name = NameInputField.text.Trim();
            string email = EmailInputField.text.Trim();

            if(email.Length > 0) {
                req = $"{{\"faction\":\"{faction}\",\"symbol\":\"{name}\",\"email\":\"{email}\"}}";
            } else {
                req = $"{{\"faction\":\"{faction}\",\"symbol\":\"{name}\"}}";
            }
            Debug.Log($"Creating agent {name} in faction {faction}");

            (ServerResult result, RegistrationResult reg) = await ServerManager.Request<RegistrationResult>("register", RequestMethod.POST, AsyncCancelToken, payload:req);
            if(result.result != ServerResult.ResultType.SUCCESS) {
                Debug.LogError($"Error creating agent:\n{result.details}");
                registerErrorTime = 2f;
                return;
            }
            SaveWarning.SetActive(true);
            TokenInputField.text = reg.data.token;
        }

        public async void CheckAuthorization( bool useSavedToken = false ) {
            string authToken = useSavedToken ? PlayerPrefs.GetString("AuthToken") : TokenInputField.text;
            (ServerResult result, AgentInfo agentInfo) = await ServerManager.Request<AgentInfo>("my/agent", RequestMethod.GET, AsyncCancelToken, null, authToken);
            if(result.result != ServerResult.ResultType.SUCCESS || agentInfo == null) {
                Debug.LogError($"Error logging in:\n{result.details}");
                loginErrorTime = 2f;
                return;
            }
            CacheHandler.Save("my/agent", JsonConvert.SerializeObject(agentInfo), new System.TimeSpan(0, 1, 0));
            if(useSavedToken == false) {
                PlayerPrefs.SetString("AuthToken", authToken);
                PlayerPrefs.Save();
            }
            SceneManager.LoadScene(1); // Main Scene.
        }
    }
}
