using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceTraders {
    public class AuthCheck : MonoBehaviour
    {
        public MapManager mapManager;
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
                mapManager.enabled = true;
                GameObject.Destroy(this.gameObject);
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
            object obj = ServerManager.Get<object>("/my/agent", authToken);
            if(obj == null) {
                errorTime = 2f;
                return;
            } else {
                PlayerPrefs.SetString("AuthToken", authToken);
                PlayerPrefs.Save();
                mapManager.enabled = true;
                GameObject.Destroy(this.gameObject);
            }
        }
    }
}
