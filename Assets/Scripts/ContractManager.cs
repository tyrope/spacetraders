using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class ContractManager : MonoBehaviour
    {
        public static List<string> Contracts = new List<string>();
        private static readonly CancellationTokenSource AsyncCancel = new CancellationTokenSource();

        // Start is called before the first frame update
        void Start() {
            LoadContracts();
        }

        private void OnDestroy() {
            AsyncCancel?.Cancel();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        private async void LoadContracts() {
            (ServerResult result, List<Contract> contractList) = await ServerManager.RequestList<Contract>("my/contracts", new System.TimeSpan(0, 1, 0), RequestMethod.GET, AsyncCancel.Token);
            if(AsyncCancel.IsCancellationRequested || result.result != ServerResult.ResultType.SUCCESS) { return; }
            foreach(Contract contract in contractList) {
                Contracts.Add(contract.id);
            }
        }

        public static async Task<Contract> GetContract( string ID ) {
            (ServerResult result, Contract contract) = await ServerManager.RequestSingle<Contract>("my/contracts/" + ID, new System.TimeSpan(0, 1, 0), RequestMethod.GET, AsyncCancel.Token);
            if(AsyncCancel.IsCancellationRequested || result.result != ServerResult.ResultType.SUCCESS) { return null; }
            return contract;
        }
    }
}
