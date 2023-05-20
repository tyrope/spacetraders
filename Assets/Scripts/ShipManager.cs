using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class ShipManager : MonoBehaviour
    {
        public static List<string> Ships = new List<string>();

        private static readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        // Start is called before the first frame update
        void Start() {
            LoadShips();
        }
        void OnDestroy() {
            AsyncCancelToken?.Cancel();
        }
        void OnApplicationQuit() {
            OnDestroy();
        }
        private async void LoadShips() {
            (ServerResult result, List<Ship> shipList) = await ServerManager.CachedRequest<List<Ship>>("my/ships", new System.TimeSpan(0, 1, 0), RequestMethod.GET, AsyncCancelToken);
            if(result.result != ServerResult.ResultType.SUCCESS) { return; }
            foreach(Ship ship in shipList) { Ships.Add(ship.symbol); }
        }
        public static async Task<Ship> GetShip( string symbol ) {
            (ServerResult result, Ship ship) = await ServerManager.CachedRequest<Ship>("my/ships/" + symbol, new System.TimeSpan(0, 0, 10), RequestMethod.GET, AsyncCancelToken);
            if(result.result != ServerResult.ResultType.SUCCESS) { return null; }
            return ship;
        }
    }
}
