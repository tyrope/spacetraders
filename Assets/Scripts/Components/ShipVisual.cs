using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class ShipVisual : OrbitalVisual
    {
        public Ship ship;
        private readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        private float TimeSinceRefresh = 0f;

        // Start is called before the first frame update
        protected override void Start() {
            transform.gameObject.name = ship.symbol;
            transform.gameObject.GetComponentInChildren<TMPro.TMP_Text>().text = ship.registration.name;

            OrbitalAltitude = 1f;
            base.Start();
        }

        // Update is called once per frame
        protected override async void Update() {
            // Update ship info.
            TimeSinceRefresh += Time.deltaTime;
            if(TimeSinceRefresh > 1f) {
                TimeSinceRefresh %= 1f;
                TimeSpan expiry;
                if(ship.nav.status == ShipNavigation.Status.IN_TRANSIT) {
                    expiry = new TimeSpan(0, 0, 1);
                } else {
                    expiry = new TimeSpan(0, 0, 10);
                }
                (ServerResult res, Ship sh) = await ServerManager.RequestSingle<Ship>($"my/ships/{ship.symbol}", expiry, RequestMethod.GET, AsyncCancelToken);
                if(AsyncCancelToken.IsCancellationRequested) { return; }
                if(res.result == ServerResult.ResultType.SUCCESS) {
                    ship = sh;
                }
            }

            // Deal with positioning.
            base.Update();
            await SetPosition();
        }

        void OnDestroy() {
            AsyncCancelToken.Cancel();
        }

        void OnApplicationQuit() {
            OnDestroy();
        }

        private async Task SetPosition() {
            transform.localScale = Vector3.one; // Always assume we can be seen.

            // No selection, display on the Galaxy level.
            if(mapManager.SelectedSystem == null && mapManager.SelectedWaypoint == null) {
                await SetGalacticPosition();
                return;
            }

            // System selected.
            if(mapManager.SelectedSystem != null) {
                // Check if we're in this system.
                if(ship.nav.status != ShipNavigation.Status.IN_TRANSIT) {
                    // We're parked... here?
                    if(mapManager.SelectedSystem.symbol == ship.nav.systemSymbol) {
                        await SetSolarPosition();
                        return;
                    }
                } else {
                    // We're in transit, is it an intra-system transit within the selected system?
                    if(mapManager.SelectedSystem.symbol == ship.nav.route.departure && mapManager.SelectedSystem.symbol == ship.nav.route.destination) {
                        await SetSolarPosition();
                        return;
                    }
                }
                // We're not in the selected system, display on the Galaxy level.
                await SetGalacticPosition();
                return;
            }

            // A waypoint is selected. Easy out: Are we parked there?
            if(ship.nav.status != ShipNavigation.Status.IN_TRANSIT && mapManager.SelectedWaypoint.symbol == ship.nav.waypointSymbol) {
                await SetSolarPosition();
                return;
            }

            bool deptWithinOrbitals = ship.nav.route.departure == mapManager.SelectedWaypoint.symbol;
            bool destWithinOrbitals = ship.nav.route.destination == mapManager.SelectedWaypoint.symbol;
            //Not parked at the selection... Grab it's orbitals.
            foreach(Waypoint.Orbital o in mapManager.SelectedWaypoint.orbitals) {
                // Are we parked?
                if(ship.nav.status != ShipNavigation.Status.IN_TRANSIT) {
                    if(o.symbol == ship.nav.waypointSymbol) {
                        // We're parked here! Display.
                        await SetSolarPosition();
                        return;
                    } else {
                        // We're not parked here. Next!
                        continue;
                    }
                }

                // We're in transit.
                deptWithinOrbitals = deptWithinOrbitals || o.symbol == ship.nav.route.departure;
                destWithinOrbitals = destWithinOrbitals || o.symbol == ship.nav.route.destination;
                if(deptWithinOrbitals && destWithinOrbitals) {
                    // And both departure and destination is within view.
                    await SetSolarPosition();
                    return;
                }
            }

            // We're not within system view, display on the Galaxy level.
            await SetGalacticPosition();
            return;
        }

        private async Task SetGalacticPosition() {
            ServerResult res;

            SolarSystem departureSystem;
            (res, departureSystem) = await ServerManager.RequestSingle<SolarSystem>($"systems/{ship.nav.route.departure}", new TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
            if(AsyncCancelToken.IsCancellationRequested || res.result != ServerResult.ResultType.SUCCESS) { return; }

            SolarSystem destinationSystem;
            (res, destinationSystem) = await ServerManager.RequestSingle<SolarSystem>($"systems/{ship.nav.route.destination}", new TimeSpan(1, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
            if(AsyncCancelToken.IsCancellationRequested || res.result != ServerResult.ResultType.SUCCESS) { return; }

            Vector2 currPos;
            Vector2 from = new Vector2(departureSystem.x, departureSystem.y);
            Vector2 to = new Vector2(destinationSystem.x, destinationSystem.y);
            if(from == to || ship.nav.FractionFlightComplete == 1) {
                currPos = to;
            } else if(ship.nav.FractionFlightComplete == 0) {
                currPos = from;
            } else {
                float dist = Vector2.Distance(to, from) * ship.nav.FractionFlightComplete;
                currPos = Vector2.Lerp(from, to, dist);
            }
            try {
                transform.position = mapManager.GetWorldSpaceFromCoords(currPos);
            } catch(ArgumentOutOfRangeException) {
                // Quick, hide!
                transform.localScale = Vector3.zero;
            }
            return;
        }

        private async Task SetSolarPosition() {
            Transform parentContainer = mapManager.SelectedWaypoint != null ? mapManager.WaypointContainer : mapManager.SystemContainer.Find(ship.nav.systemSymbol).Find("Waypoints").transform;

            if(ship.nav.status != ShipNavigation.Status.IN_TRANSIT) {
                // Parked!
                foreach(Transform wpTrans in parentContainer) {
                    if(wpTrans.gameObject.GetComponent<WaypointVisual>().waypoint.symbol == ship.nav.waypointSymbol) {
                        transform.localScale = Vector3.one;
                        transform.position = new Vector3(wpTrans.position.x, 0, wpTrans.position.z);
                        transform.Find("Visuals").rotation = Quaternion.identity;

                        if(ship.nav.status == ShipNavigation.Status.IN_ORBIT && mapManager.GetMapScale() > 0.05f) {
                            // We're orbiting around the currently selected waypoint; actually show an orbit.
                            transform.position += OrbitalPosition;
                            transform.Find("Visuals").Rotate(Vector3.up, OrbitalAngle - 90f);
                        }
                        // Park ourselves *above* the waypoint we're at.
                        transform.position += Vector3.up * 0.5f;
                        return;
                    }
                }
            } else {
                // TODO Proper calculation of route from orbital A to orbital B, because this solution is mucho wonky.
                Transform departure = null;
                Transform destination = null;
                string wpSymbol;
                foreach(Transform wpTrans in parentContainer) {
                    wpSymbol = wpTrans.gameObject.GetComponent<WaypointVisual>().waypoint.symbol;
                    if(wpSymbol == ship.nav.route.departure) { departure = wpTrans; } // Is this the departure waypoint?
                    if(wpSymbol == ship.nav.route.destination) { destination = wpTrans; } // Is this the destination waypoint?
                    if(departure != null && destination != null) { break; } // We've got both ends! Stop looping.
                }

                if(departure == null || destination == null) {
                    Debug.LogError($"Tried to SetSolarPosition of {ship.registration.name} but it's out of bounds.");
                    await SetGalacticPosition();
                    return;
                }

                transform.Find("Visuals").LookAt(destination); // Turn to face.
                transform.Find("Visuals").Rotate(Quaternion.Euler(-90, 0, 0).eulerAngles); // Except the model's rotated.
                Vector3 pos = Vector3.Lerp(departure.position, destination.position, ship.nav.FractionFlightComplete);
                transform.position = pos + Vector3.up * 0.5f;
            }
        }
    }
}
