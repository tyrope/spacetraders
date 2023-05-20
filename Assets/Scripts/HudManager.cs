using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace STCommander
{
    public class HudManager : MonoBehaviour
    {
        public GameObject HUD;
        public GameObject ShipPrefab;
        public GameObject ContractPrefab;

        private TMP_Text AgentInfoDisplay;
        private RectTransform FleetTransform;
        private RectTransform ContractsTransform;
        private float timeSinceLastUpdate = Mathf.Infinity;

        private readonly Dictionary<string, GameObject> ShipGOs = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> ContractGOs = new Dictionary<string, GameObject>();
        private readonly CancellationTokenSource asyncCancelToken = new CancellationTokenSource();
        private readonly float timeBetweenUpdates = 1f;

        // Start is called before the first frame update
        void Start() {
            AgentInfoDisplay = HUD.transform.Find("AgentInfo").GetComponent<TMP_Text>();
            FleetTransform = (RectTransform) HUD.transform.Find("Fleet");
            ContractsTransform = (RectTransform) HUD.transform.Find("Contracts");
        }

        // Update is called once per frame
        void Update() {
            // Rate limit
            if(timeSinceLastUpdate < timeBetweenUpdates) {
                timeSinceLastUpdate += Time.deltaTime;
                return;
            }
            timeSinceLastUpdate = 0;

            UpdateAgentInfo();
            UpdateFleetHUD();
            UpdateContractHUD();
        }

        private void OnDestroy() {
            asyncCancelToken.Cancel();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        private async void UpdateAgentInfo() {
            (bool success, AgentInfo info) = await ServerManager.CachedRequest<AgentInfo>("my/agent", new System.TimeSpan(0,1,0), RequestMethod.GET, asyncCancelToken);
            if(asyncCancelToken.IsCancellationRequested == false && success) {
                AgentInfoDisplay.text = $"Admiral {info.symbol} - Account balance: {info.credits:n0}Cr";
            }
        }

        private async void UpdateFleetHUD() {
            foreach(string shipSymbol in ShipManager.Ships){
                if(!ShipGOs.ContainsKey(shipSymbol)) {
                    ShipGOs.Add(shipSymbol, SpawnShip(shipSymbol));
                }
                UpdateShipInfo(shipSymbol);
                await Task.Yield();
                if(asyncCancelToken.IsCancellationRequested) { return; }
            }
        }

        private GameObject SpawnShip( string symbol ) {
            GameObject go = Instantiate(ShipPrefab, FleetTransform);
            go.name = symbol;
            return go;
        }

        private async void UpdateShipInfo( string shipSymbol ) {
            Transform trans = ShipGOs[shipSymbol].transform;
            Ship ship = await ShipManager.GetShip(shipSymbol);
            if(asyncCancelToken.IsCancellationRequested) { return; }
            trans.Find("Registration").GetComponent<TMP_Text>().text = ship.registration.name;
            trans.Find("Role").GetComponent<TMP_Text>().text = ship.registration.role.ToString();
            trans.Find("Cargo").GetComponent<TMP_Text>().text = $"Cargo: {ship.cargo.units / (float) ship.cargo.capacity * 100f:n2}%\n{ship.cargo.units}/{ship.cargo.capacity}";
            trans.Find("Fuel").GetComponent<TMP_Text>().text = $"Fuel: {ship.fuel.current / (float) ship.fuel.capacity * 100f:n2}%\n{ship.fuel.current}/{ship.fuel.capacity}";

            string navString;
            switch(ship.nav.status) {
                case Ship.Navigation.Status.DOCKED:
                    navString = $"DOCKED @ {ship.nav.waypointSymbol}";
                    break;
                case Ship.Navigation.Status.IN_ORBIT:
                    navString = $"ORBITING {ship.nav.waypointSymbol}";
                    break;
                case Ship.Navigation.Status.IN_TRANSIT:
                    DateTime ArrivalTime = DateTime.Parse(ship.nav.route.arrival);
                    string ETA = (ArrivalTime - DateTime.Now).ToString("HH:mm:ss");
                    navString = $"{ship.nav.route.departure}→{ship.nav.route.destination} ({ETA})";
                    break;
                default:
                    navString = "ERR_INVALID_NAV_STATUS";
                    break;
            }
            trans.Find("Navigation").GetComponent<TMP_Text>().text = navString;
        }

        private async void UpdateContractHUD() {
            foreach(string ID in ContractManager.Contracts) {
                if(!ContractGOs.ContainsKey(ID)) {
                    ContractGOs.Add(ID, SpawnContract(ID));
                }
                UpdateContractInfo(ID);
                await Task.Yield();
                if(asyncCancelToken.IsCancellationRequested) { return; }
            }
        }
        private GameObject SpawnContract( string ID ) {
            GameObject go = Instantiate(ContractPrefab, ContractsTransform);
            go.name = ID;
            return go;
        }

        private async void UpdateContractInfo(string ID ) {
            Transform trans = ContractGOs[ID].transform;
            Contract contract = await ContractManager.GetContract(ID);
            if(asyncCancelToken.IsCancellationRequested) { return; }
            trans.Find("Content").GetComponent<TMP_Text>().text = contract.ToString();
        }
    }
}
