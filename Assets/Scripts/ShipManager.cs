using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class ShipManager : MonoBehaviour
    {
        public static readonly List<string> Ships = new List<string>();

        private static readonly CancellationTokenSource AsyncCancel = new CancellationTokenSource();

        // Start is called before the first frame update
        void Start() {
            if(Ships.Count == 0) {
                LoadShips();
            }
        }

        void OnDestroy() {
            AsyncCancel?.Cancel();
        }

        void OnApplicationQuit() {
            OnDestroy();
        }

        public static async void LoadShips() {
            (ServerResult result, List<Ship> shipList) = await ServerManager.RequestList<Ship>("my/ships/?limit=20", new System.TimeSpan(0, 1, 0), RequestMethod.GET, AsyncCancel.Token);
            if(AsyncCancel.IsCancellationRequested) { return; }
            if(result.result != ServerResult.ResultType.SUCCESS) { return; }
            foreach(Ship ship in shipList) { Ships.Add(ship.symbol); }
        }

        public static async Task<Ship> GetShip( string symbol ) {
            (ServerResult result, Ship ship) = await ServerManager.RequestSingle<Ship>("my/ships/" + symbol, new System.TimeSpan(0, 0, 10), RequestMethod.GET, AsyncCancel.Token);
            if(AsyncCancel.IsCancellationRequested) { return null; }
            if(result.result != ServerResult.ResultType.SUCCESS) { return null; }
            return ship;
        }
    }
}
