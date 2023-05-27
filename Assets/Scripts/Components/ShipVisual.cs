using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class ShipVisual : MonoBehaviour
    {
        public MapManager mapManager;
        public Ship ship;
        private readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        private float TimeSinceRefresh = 0f;
        private float OrbitTime = 0f;
        private float OrbitalAngle => (OrbitTime / 1538.18f) * 360f;
        private Vector3 OrbitalPosition => new Vector3(Mathf.Sin(OrbitalAngle), 0, Mathf.Cos(OrbitalAngle));

        // Start is called before the first frame update
        void Start() {
            OrbitTime = (float) (DateTime.UtcNow - DateTime.MinValue).TotalSeconds;
            transform.gameObject.name = ship.symbol;
            transform.gameObject.GetComponentInChildren<TMPro.TMP_Text>().text = ship.registration.name;
        }

        // Update is called once per frame
        private async void Update() {
            TimeSinceRefresh += Time.deltaTime;
            if(TimeSinceRefresh > 1f) {
                TimeSinceRefresh %= 1f;
                (ServerResult res, Ship sh) = await ServerManager.CachedRequest<Ship>($"my/ships/{ship.symbol}", new TimeSpan(0, 0, 10), RequestMethod.GET, AsyncCancelToken);
                if(AsyncCancelToken.IsCancellationRequested) { return; }
                if(res.result == ServerResult.ResultType.SUCCESS) {
                    ship = sh;
                }
            }
            OrbitTime += Time.deltaTime;
            OrbitTime %= 1538.18f;
            await SetPosition();
        }
        void OnDestroy() {
            AsyncCancelToken.Cancel();
        }

        void OnApplicationQuit() {
            OnDestroy();
        }

        private async Task SetPosition() {
            ServerResult res;
            SolarSystem deptSys;
            SolarSystem destSys;
            (res, destSys) = await ServerManager.CachedRequest<SolarSystem>($"systems/{ship.nav.route.destination.systemSymbol}", new TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
            if(AsyncCancelToken.IsCancellationRequested || res.result != ServerResult.ResultType.SUCCESS) { return; }
            (res, deptSys) = await ServerManager.CachedRequest<SolarSystem>($"systems/{ship.nav.route.departure.systemSymbol}", new TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
            if(AsyncCancelToken.IsCancellationRequested || res.result != ServerResult.ResultType.SUCCESS) { return; }

            switch(ship.nav.status) {
                // An in-transit ship is only ever shown on the Galaxy level.
                case Ship.Navigation.Status.IN_TRANSIT:
                    Vector2 currPos;
                    Vector2 from = new Vector2(deptSys.x, deptSys.y);
                    Vector2 to = new Vector2(destSys.x, destSys.y);
                    if(from == to || ship.nav.route.FractionFlightComplete == 0) {
                        currPos = from;
                    } else if(ship.nav.route.FractionFlightComplete == 1) {
                        currPos = to;
                    } else {
                        currPos = from + (to - from) * ship.nav.route.FractionFlightComplete;
                    }
                    try {
                        transform.position = mapManager.GetWorldSpaceFromCoords(currPos);
                        transform.localScale = Vector3.one;
                    } catch(ArgumentOutOfRangeException) {
                        transform.localScale = Vector3.zero;
                    }
                    return;
                default: // Stationary. Are we within the selected bodies?
                    if(mapManager.SelectedSystem == null && mapManager.SelectedWaypoint == null) {
                        // No selection, display on the Galaxy level.
                        goto case Ship.Navigation.Status.IN_TRANSIT;
                    }

                    if(mapManager.SelectedSystem != null) {
                        if(mapManager.SelectedSystem.symbol != ship.nav.systemSymbol) {
                            // We're not in the selected system, display on the Galaxy level.
                            goto case Ship.Navigation.Status.IN_TRANSIT;
                        }
                    } else {
                        // A waypoint is selected, grab it's orbitals.
                        bool withinDisplayedWaypoints = false;
                        foreach(Waypoint.Orbital o in mapManager.SelectedWaypoint.orbitals) {
                            if(o.symbol == ship.nav.waypointSymbol) {
                                withinDisplayedWaypoints = true;
                                break;
                            }
                        }

                        // We're not within the selection, display on the Galaxy level.
                        if(withinDisplayedWaypoints == false && mapManager.SelectedWaypoint.symbol != ship.nav.waypointSymbol) {
                            goto case Ship.Navigation.Status.IN_TRANSIT;
                        }
                    }

                    // We're on the System level.
                    Transform parentContainer = mapManager.SelectedWaypoint != null ? mapManager.WaypointContainer : mapManager.SystemContainer.Find(ship.nav.systemSymbol).Find("Waypoints").transform;
                    foreach(Transform wpTrans in parentContainer) {
                        if(wpTrans.gameObject.GetComponent<WaypointVisual>().waypoint.symbol == ship.nav.waypointSymbol) {
                            transform.localScale = Vector3.one;
                            transform.position = new Vector3(wpTrans.position.x, 0.5f, wpTrans.position.z);
                            transform.rotation = Quaternion.identity;
                            if(ship.nav.status == Ship.Navigation.Status.IN_ORBIT) {
                                transform.position += OrbitalPosition * mapManager.GetMapScale();
                                transform.Rotate(Vector3.up, OrbitalAngle + 90f);
                            }
                            return;
                        }
                    }
                    return;
            }
        }
    }
}
