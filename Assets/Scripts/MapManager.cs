using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

namespace STCommander
{
    public class MapManager : MonoBehaviour
    {
        public GameObject SystemPrefab;
        public GameObject WaypointPrefab;
        public GameObject shipPrefab;
        public TMPro.TMP_Text MapLegend;

        public SolarSystem SelectedSystem { get; private set; }
        public Waypoint SelectedWaypoint { get; private set; }

        public Transform SystemContainer { get; private set; }
        public Transform WaypointContainer { get; private set; }
        private Transform ShipContainer;
        private List<SolarSystem> solarSystems;
        private Vector2 mapCenter = Vector2.zero;
        private float zoom;
        private int displayedSystems;

        private readonly Dictionary<SolarSystem, GameObject> solarSystemObjects = new Dictionary<SolarSystem, GameObject>();
        private readonly CancellationTokenSource AsyncCancelToken = new CancellationTokenSource();

        void Start() {
            SystemContainer = new GameObject("SystemContainer").transform;
            SystemContainer.position = Vector3.zero;
            SystemContainer.parent = gameObject.transform.parent;
            WaypointContainer = new GameObject("WaypointContainer").transform;
            WaypointContainer.position = Vector3.zero;
            WaypointContainer.parent = gameObject.transform.parent;
            CreateMap();
            SpawnShips();
        }
        public void ParseInputs() {
            // TODO Deselect using in-world stuff, not rightclick.
            if(Input.GetKeyDown(KeyCode.Mouse1)) { Deselect(); }
            MapControls();
        }
        private void OnDestroy() {
            AsyncCancelToken.Cancel();
        }
        private void OnApplicationQuit() {
            OnDestroy();
        }

        private async void MapControls() {
            /// Reset ///
            if(Input.GetKeyDown(KeyCode.Keypad5)) {
                await CenterMapOnHQ();
                RefreshMap();
                return;
            }
            /// Scroll ///
            float scrollSpeed = 0.5f;
            Vector2 panDir = Vector2.zero;
            if(Input.GetKey(KeyCode.Keypad1)) { panDir += Vector2.down + Vector2.left; }
            if(Input.GetKey(KeyCode.Keypad2)) { panDir += Vector2.down; }
            if(Input.GetKey(KeyCode.Keypad3)) { panDir += Vector2.down + Vector2.right; }
            if(Input.GetKey(KeyCode.Keypad4)) { panDir += Vector2.left; }
            if(Input.GetKey(KeyCode.Keypad6)) { panDir += Vector2.right; }
            if(Input.GetKey(KeyCode.Keypad7)) { panDir += Vector2.up + Vector2.left; }
            if(Input.GetKey(KeyCode.Keypad8)) { panDir += Vector2.up; }
            if(Input.GetKey(KeyCode.Keypad9)) { panDir += Vector2.up + Vector2.right; }
            mapCenter += panDir.normalized * Time.deltaTime * scrollSpeed * zoom;
            /// ZOOOOOOOOOMIES ///
            int zoomDir = 0;
            if(Input.GetKey(KeyCode.KeypadMinus)) { zoomDir++; }
            if(Input.GetKey(KeyCode.KeypadPlus)) { zoomDir--; }

            zoom += Time.deltaTime * zoomDir * Mathf.Pow(zoom, 1.1f);
            zoom = Mathf.Clamp(zoom, 10, 5000); // No zooming to stupid numbers.
            /// Refresh if needed ///
            if(panDir != Vector2.zero || zoomDir != 0) { RefreshMap(); }
        }

        public float GetZoom() => zoom;

        public Vector2 GetCenter() => mapCenter;

        public float GetMapScale() {
            if(SelectedSystem != null) {
                float maxMagnitude = 0f;
                foreach(Waypoint wp in SelectedSystem.waypoints) {
                    float mag = new Vector2(wp.x, wp.y).magnitude;
                    if(mag > maxMagnitude) { maxMagnitude = mag; }
                }
                return 2.0f / maxMagnitude;
            } else if(SelectedWaypoint == null || SelectedWaypoint.orbitals == null) {
                return 0.75f;
            }
            return 1.5f / (float) (2f + SelectedWaypoint.orbitals.Length);
        }

        private (Vector2, Vector2) GetMapBounds() {
            Vector2 minBounds = new Vector2(zoom * -1 + mapCenter.x, zoom * -1 + mapCenter.y);
            Vector2 maxBounds = new Vector2(zoom + mapCenter.x, zoom + mapCenter.y);
            return (minBounds, maxBounds);
        }

        public Vector3 GetWorldSpaceFromCoords( float x, float y ) {
            return GetWorldSpaceFromCoords(new Vector2(x, y));
        }

        public Vector3 GetWorldSpaceFromCoords( Vector2 position ) {
            (Vector2 minBounds, Vector2 maxBounds) = GetMapBounds();
            if(minBounds.x > position.x || position.x > maxBounds.x || minBounds.y > position.y || position.y > maxBounds.y) {
                throw new ArgumentOutOfRangeException("position", "This location is outside of the map.");
            }

            float xPos = position.x + mapCenter.x * -1;
            float yPos = position.y + mapCenter.y * -1;
            return new Vector3(xPos, 0, yPos) / GetZoom() * 2f;
        }

        private async void CreateMap( int retries = 0 ) {
            // Load the galaxy
            ServerResult result;
            (result, solarSystems) = await ServerManager.CachedRequest<List<SolarSystem>>("systems.json", new System.TimeSpan(7, 0, 0, 0), RequestMethod.GET, AsyncCancelToken);
            if(AsyncCancelToken.IsCancellationRequested) { return; }
            if(result.result != ServerResult.ResultType.SUCCESS) {
                Debug.LogError($"Failed to load systems.json\n{result}");
                if(retries < 5) {
                    await Task.Delay(1000);
                    if(AsyncCancelToken.IsCancellationRequested) { return; }
                    CreateMap(retries + 1);
                    return;
                } else {
                    Debug.LogError("5 failed attempts to load systems.json, something is seriously wrong.");
                    return;
                }
            }

            // Center on the Player HQ.
            await CenterMapOnHQ();
            MapLegend.text = $"[{Mathf.CeilToInt(mapCenter.x)},{Mathf.CeilToInt(mapCenter.y)}]\n1:{zoom:n0}";

            // Create the game objects.
            GameObject go;
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Reset();
            stopwatch.Start();
            (Vector2 minBounds, Vector2 maxBounds) = GetMapBounds();
            foreach(SolarSystem sys in solarSystems) {
                // Skip out-of-bounds systems.
                if(sys.x < minBounds.x || sys.x > maxBounds.x || sys.y < minBounds.y || sys.y > maxBounds.y) { continue; }

                displayedSystems++;
                go = SpawnSystem(sys);
                solarSystemObjects.Add(sys, go);
                if(stopwatch.ElapsedMilliseconds > 16.666f) { // 1000 / 60, rounded down.
                    stopwatch.Stop();
                    stopwatch.Reset();
                    await Task.Yield();
                    if(AsyncCancelToken.IsCancellationRequested) { return; }
                    stopwatch.Start();
                }
            }
            stopwatch.Stop();
            Debug.Log($"Map loaded! {displayedSystems} within range.");
        }

        private void RefreshMap() {
            (Vector2 minBounds, Vector2 maxBounds) = GetMapBounds();
            MapLegend.text = $"[{Mathf.CeilToInt(mapCenter.x)},{Mathf.CeilToInt(mapCenter.y)}]\n1:{zoom:n0}";

            // Go through each solar system to check the bounds..
            foreach(SolarSystem sys in solarSystems) {
                if(sys.x < minBounds.x || sys.x > maxBounds.x || sys.y < minBounds.y || sys.y > maxBounds.y) {
                    // Out of bounds.
                    if(solarSystemObjects.ContainsKey(sys)) {
                        // Despawn
                        GameObject.Destroy(solarSystemObjects[sys]);
                        solarSystemObjects.Remove(sys);
                        displayedSystems--;
                    }
                } else {
                    // In bounds.
                    if(solarSystemObjects.ContainsKey(sys) == false) {
                        // Didn't exist, spawn now.
                        solarSystemObjects.Add(sys, SpawnSystem(sys));
                        displayedSystems++;
                    } else {
                        solarSystemObjects[sys].GetComponent<SolarSystemVisual>().SetPosition();
                    }
                }
            }
        }

        private async Task CenterMapOnHQ() {
            // Center on the Player HQ.
            (ServerResult result, AgentInfo agent) = await ServerManager.CachedRequest<AgentInfo>("my/agent", new System.TimeSpan(0, 1, 0), RequestMethod.GET, AsyncCancelToken);
            if(AsyncCancelToken.IsCancellationRequested) { return; }
            if(result.result == ServerResult.ResultType.SUCCESS) {
                // Query the HQ waypoint for system name.
                SolarSystem hq;
                string hqSystem = agent.headquarters.Substring(0, agent.headquarters.LastIndexOf('-'));
                (result, hq) = await ServerManager.CachedRequest<SolarSystem>($"systems/{hqSystem}", new System.TimeSpan(1, 0, 0), RequestMethod.GET, AsyncCancelToken);
                if(AsyncCancelToken.IsCancellationRequested) { return; }
                if(result.result != ServerResult.ResultType.SUCCESS) { Debug.LogError($"Failed to load Player HQ.\n{result}"); return; }
                mapCenter = new Vector2(hq.x, hq.y);
            }

            zoom = 500;
        }

        public async void SelectSystem( SolarSystem sys ) {
            // Selecting the already selected system breaks things, so don't be stupid.
            if(sys == SelectedSystem) { return; }

            // Deselect
            if(SelectedSystem != null && solarSystemObjects.ContainsKey(SelectedSystem)) {
                //Tell the SSV it's been deselected.
                solarSystemObjects[SelectedSystem].GetComponent<SolarSystemVisual>().ChangeSelect(false);

                //Nuke the waypoints of the previously selected system.
                foreach(Transform t in solarSystemObjects[SelectedSystem].transform.Find("Waypoints")) {
                    Destroy(t.gameObject);
                }
            }

            if(sys == null) {
                SelectedSystem = null;
                return;
            }
            SelectedSystem = sys;

            // Recenter the map.
            mapCenter = new Vector2(SelectedSystem.x, SelectedSystem.y);
            RefreshMap();

            // Load the waypoints.
            Waypoint wp;
            ServerResult result;
            for(int i = 0; i < sys.waypoints.Count; i++) {
                (result, wp) = await ServerManager.CachedRequest<Waypoint>($"systems/{sys.symbol}/waypoints/{sys.waypoints[i].symbol}", new System.TimeSpan(1, 0, 0), RequestMethod.GET, AsyncCancelToken);
                if(AsyncCancelToken.IsCancellationRequested) { return; }
                if(result.result != ServerResult.ResultType.SUCCESS) {
                    Debug.LogError($"Failed to load waypoint {sys.waypoints[i].symbol}\n{result}");
                    // Skip waypoints that error for some reason.
                    continue;
                }
                // Update the system in memory.
                if(wp != sys.waypoints[i]) { sys.waypoints[i] = wp; }
                // Send it.
                SpawnWaypoint(wp, solarSystemObjects[sys].transform.Find("Waypoints"));
            }

            // Tell the new system's visual it's been selected.
            solarSystemObjects[SelectedSystem].GetComponent<SolarSystemVisual>().ChangeSelect(true);
        }

        public async void SelectWaypoint( Waypoint wp ) {
            // Selecting the already selected waypoint might break things, so don't be stupid.
            if(wp == SelectedWaypoint) { return; }

            // Deselect the currently selected system, if any.
            if(SelectedSystem != null && wp != null) {
                SelectSystem(null);
            }
            // Delete all waypoint objects if we're deselecting.
            if(SelectedWaypoint != null) {
                foreach(Transform wpobj in WaypointContainer) {
                    Destroy(wpobj.gameObject);
                }
            }
            if(wp == null) {
                // Select the parent system.
                foreach(SolarSystem s in solarSystems) {
                    if(s.symbol == SelectedWaypoint.systemSymbol) {
                        SelectedWaypoint = null;
                        SelectSystem(s);
                        return;
                    }
                }
            }
            SelectedWaypoint = wp;

            // Grab the latest info...
            (ServerResult result, Waypoint updatedWp) = await ServerManager.CachedRequest<Waypoint>($"systems/{wp.systemSymbol}/waypoints/{wp.symbol}", new System.TimeSpan(1, 0, 0), RequestMethod.GET, AsyncCancelToken);
            if(AsyncCancelToken.IsCancellationRequested) { return; }
            if(result.result == ServerResult.ResultType.SUCCESS) {
                wp = updatedWp;
            } // Else: Just use the old data...

            // Load the main selected object and let it know it's The Chosen One.
            SpawnWaypoint(wp, WaypointContainer).GetComponent<WaypointVisual>().SetSelected(true);

            Waypoint orbital;
            for(int i = 0; i < wp.orbitals.Length; i++) {
                (result, orbital) = await ServerManager.CachedRequest<Waypoint>($"systems/{wp.systemSymbol}/waypoints/{wp.orbitals[i].symbol}", new System.TimeSpan(1, 0, 0), RequestMethod.GET, AsyncCancelToken);
                if(AsyncCancelToken.IsCancellationRequested) { return; }
                if(result.result != ServerResult.ResultType.SUCCESS) {
                    Debug.LogError($"Failed to load waypoint {wp.orbitals[i].symbol}\n{result}");
                    // Skip waypoints that error for some reason.
                    continue;
                }
                SpawnWaypoint(orbital, WaypointContainer);
            }
        }
        public void Deselect() {
            if(SelectedWaypoint != null) {
                SelectWaypoint(null);
            } else if(SelectedSystem != null) {
                SelectSystem(null);
            }
        }

        private GameObject SpawnSystem( SolarSystem sys ) {
            GameObject system = Instantiate(SystemPrefab);
            system.transform.parent = SystemContainer;
            SolarSystemVisual sd = system.GetComponent<SolarSystemVisual>();
            sd.system = sys;
            sd.MapManager = this;

            system.SetActive(true);
            return system;
        }

        private GameObject SpawnWaypoint( Waypoint waypoint, Transform parent ) {
            GameObject go = GameObject.Instantiate(WaypointPrefab);
            WaypointVisual wpvisual = go.GetComponent<WaypointVisual>();
            go.transform.parent = parent;

            wpvisual.waypoint = waypoint;
            wpvisual.MapManager = this;

            go.SetActive(true);
            return go;
        }

        private async void SpawnShips() {
            ShipContainer = new GameObject("ShipContainer").transform;
            ShipContainer.position = Vector3.zero;
            ShipContainer.parent = gameObject.transform.parent;

            while(ShipManager.Ships.Count == 0) {
                // Give the ShipManager a frame to fill it's ship list.
                await Task.Yield();
            }
            foreach(string ship in ShipManager.Ships) {
                GameObject go = Instantiate(shipPrefab, Vector3.zero, Quaternion.identity, ShipContainer);
                ShipVisual sv = go.GetComponentInChildren<ShipVisual>();
                sv.mapManager = this;
                sv.ship = await ShipManager.GetShip(ship);
                go.SetActive(true);
            }
        }
    }
}
