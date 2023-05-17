using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SpaceTraders
{

    public class HudManager : MonoBehaviour
    {
        public GameObject HUD;
        public ShipManager shipManager;
        public GameObject ShipPrefab;

        private readonly CancellationTokenSource asyncCancelToken = new CancellationTokenSource();

        private TMP_Text AgentInfoDisplay;
        private RectTransform FleetTransform;
        private readonly Dictionary<string, GameObject> ShipGOs = new Dictionary<string, GameObject>();

        // Start is called before the first frame update
        void Start() {
            AgentInfoDisplay = HUD.transform.Find("AgentInfo").GetComponent<TMP_Text>();
            FleetTransform = (RectTransform) HUD.transform.Find("Fleet");
        }

        // Update is called once per frame
        void Update() {
            UpdateAgentInfo();

            // TODO BROKEN!
            //UpdateFleetHUD();
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

        private async void UpdateFleetHUD() {
            foreach(string shipSymbol in shipManager.Ships){
                if(!ShipGOs.ContainsKey(shipSymbol)) {
                    ShipGOs.Add(shipSymbol, SpawnShip(shipSymbol));
                }
                UpdateShipInfo(shipSymbol);
                await Task.Yield();
            }
        }

        private async void UpdateShipInfo( string shipSymbol ) {
            Transform trans = ShipGOs[shipSymbol].transform;
            Ship ship = await shipManager.GetShip(shipSymbol);
            trans.Find("Registration").GetComponent<TMP_Text>().text = ship.registration.name;
            trans.Find("Role").GetComponent<TMP_Text>().text = ship.registration.role.ToString();
            trans.Find("Cargo").GetComponent<TMP_Text>().text = $"Cargo: {(ship.cargo.units/ship.cargo.capacity):n2}%\n{ship.cargo.units}/{ship.cargo.capacity}";
            trans.Find("Fuel").GetComponent<TMP_Text>().text = $"Fuel: {(ship.fuel.current / ship.fuel.capacity):n2}%\n{ship.fuel.current}/{ship.fuel.capacity}";
        }

        private GameObject SpawnShip(string symbol) {
            GameObject go = Instantiate(ShipPrefab, FleetTransform);
            go.name = symbol;
            return go;
        }
    }
}
