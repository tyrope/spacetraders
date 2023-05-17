using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SpaceTraders {
    public class ShipManager : MonoBehaviour
    {
        public List<string> Ships;
        private CancellationTokenSource asyncCancelToken;
        
        // Start is called before the first frame update
        void Start() {
            asyncCancelToken = new CancellationTokenSource();
            LoadShips();
        }

        private void OnDestroy() {
            asyncCancelToken?.Cancel();
        }

        private void OnApplicationQuit() {
            OnDestroy();
        }

        private async void LoadShips() {
            List<Ship> shipList = await ServerManager.CachedRequest<List<Ship>>("my/ships", new System.TimeSpan(0, 1, 0), RequestMethod.GET, asyncCancelToken);
            foreach(Ship ship in shipList) {
                Ships.Add(ship.symbol);
            }
        }

        public async Task<Ship> GetShip(string symbol) {
            return await ServerManager.CachedRequest<Ship>("my/ships/" + symbol, new System.TimeSpan(0, 1, 0), RequestMethod.GET, asyncCancelToken);
        }
    }
}