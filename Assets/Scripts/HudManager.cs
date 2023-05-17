using System.Threading;
using TMPro;
using UnityEngine;

namespace SpaceTraders
{

    public class HudManager : MonoBehaviour
    {
        public GameObject HUD;
        private readonly CancellationTokenSource asyncCancelToken = new CancellationTokenSource();

        private TMP_Text AgentInfoDisplay;
        // Start is called before the first frame update
        void Start() {
            AgentInfoDisplay = HUD.transform.Find("AgentInfo").GetComponent<TMP_Text>();
        }

        // Update is called once per frame
        void Update() {
            UpdateAgentInfo();
        }

        private void OnDestroy() {
            asyncCancelToken.Cancel();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        private async void UpdateAgentInfo() {
            AgentInfo info = await ServerManager.CachedRequest<AgentInfo>("my/agent", new System.TimeSpan(0,1,0), RequestMethod.GET, asyncCancelToken);
            AgentInfoDisplay.text = $"Admiral {info.symbol} - Account balance: {info.credits:n0}Cr";
        }
    }
}