using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceTraders {
    public class AuthCheck : MonoBehaviour
    {
        public TMPro.TMP_Text ButtonText;
        public bool RevokeAuthorization = false;
        private TMPro.TMP_InputField inputField;
        private float errorTime = 0f;

        // Start is called before the first frame update
        void Start() {
            // We're authorized already.
            if(RevokeAuthorization) {
                PlayerPrefs.DeleteKey("AuthToken");
                PlayerPrefs.Save();
            }else if(PlayerPrefs.HasKey("AuthToken")) {
                SceneManager.LoadScene(1); // Main Scene.
                return;
            }

            inputField = gameObject.GetComponentInChildren<TMPro.TMP_InputField>();
        }

        void Update() {
            if(errorTime > 0f) {
                errorTime -= Time.deltaTime;
            }
            ButtonText.color = Color.Lerp(Color.white, Color.red, errorTime / 2f);
        }

        public void CheckAuthorization() {
            string authToken = inputField.text;
            object obj = ServerManager.Request<object>(REQUEST_METHOD.GET, "/my/agent", authToken);
            if(obj == null) {
                errorTime = 2f;
                return;
            } else {
                CacheHandler.Save("my/agent", JsonConvert.SerializeObject(obj), new System.TimeSpan(1, 0, 0));
                PlayerPrefs.SetString("AuthToken", authToken);
                PlayerPrefs.Save();
                SceneManager.LoadScene(1); // Main Scene.
            }
        }
    }
}
