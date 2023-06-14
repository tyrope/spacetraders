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
        private readonly CancellationTokenSource AsyncCancel = new CancellationTokenSource();
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
            if(FleetTransform.gameObject.activeSelf) { UpdateFleetHUD(); }
            if(ContractsTransform.gameObject.activeSelf) { UpdateContractHUD(); }
        }

        private void OnDestroy() {
            AsyncCancel?.Cancel();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        private async void UpdateAgentInfo() {
            (ServerResult result, Agent info) = await ServerManager.RequestSingle<Agent>("my/agent", new System.TimeSpan(0, 1, 0), RequestMethod.GET, AsyncCancel.Token);
            if(AsyncCancel.IsCancellationRequested == false && result.result == ServerResult.ResultType.SUCCESS) {
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
                if(AsyncCancel.IsCancellationRequested) { return; }
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
            if(AsyncCancel.IsCancellationRequested) { return; }
            trans.Find("Registration").GetComponent<TMP_Text>().text = ship.registration.name;
            trans.Find("Role").GetComponent<TMP_Text>().text = ship.registration.role.ToString();
            trans.Find("Cargo").GetComponent<TMP_Text>().text = $"Cargo: {ship.cargo}";
            trans.Find("Fuel").GetComponent<TMP_Text>().text = $"Fuel: {ship.fuel}";
            trans.Find("Navigation").GetComponent<TMP_Text>().text = ship.nav.ToString();
        }

        private async void UpdateContractHUD() {
            foreach(string ID in ContractManager.Contracts) {
                if(!ContractGOs.ContainsKey(ID)) {
                    ContractGOs.Add(ID, SpawnContract(ID));
                }
                UpdateContractInfo(ID);
                await Task.Yield();
                if(AsyncCancel.IsCancellationRequested) { return; }
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
            if(AsyncCancel.IsCancellationRequested) { return; }
            trans.Find("Content").GetComponent<TMP_Text>().text = contract.ToString();
        }
    }
}
