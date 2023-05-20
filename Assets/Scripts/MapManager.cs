using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace STCommander
{
    public class MapManager : MonoBehaviour
    {
        public GameObject SystemPrefab;
        public GameObject WaypointPrefab;

        private Transform SystemContainer;
        private List<SolarSystem> solarSystems;
        private SolarSystem selectedSystem;
        private Vector2 mapCenter = Vector2.zero;
        private float zoom = 50;
        private int displayedSystems;

        private readonly Dictionary<SolarSystem, GameObject> solarSystemObjects = new Dictionary<SolarSystem, GameObject>();
        private readonly CancellationTokenSource asyncCancelToken = new CancellationTokenSource();

        void Start() {
            SystemContainer = new GameObject("SystemContainer").transform;
            SystemContainer.position = Vector3.zero;
            SystemContainer.parent = gameObject.transform.parent;
            CreateMap();
        }
        public void ParseInputs() {
            // TODO Deselect using in-world stuff, not rightclick.
            if(Input.GetKeyDown(KeyCode.Mouse1)) { DeselectSystem(); }
            MapControls();
        }
        private void OnDestroy() {
            asyncCancelToken.Cancel();
        }
        private void OnApplicationQuit() {
            OnDestroy();
        }
        private void MapControls() {
            /// Reset ///
            if(Input.GetKeyDown(KeyCode.Keypad5)) {
                mapCenter = Vector2.zero;
                zoom = 50;
                RefreshMap();
                return;
            }
            /// Scroll ///
            float scrollSpeed = 5f;
            Vector2 panDir = Vector2.zero;
            if(Input.GetKey(KeyCode.Keypad1)) { panDir += Vector2.down + Vector2.left; }
            if(Input.GetKey(KeyCode.Keypad2)) { panDir += Vector2.down; }
            if(Input.GetKey(KeyCode.Keypad3)) { panDir += Vector2.down + Vector2.right; }
            if(Input.GetKey(KeyCode.Keypad4)) { panDir += Vector2.left; }
            if(Input.GetKey(KeyCode.Keypad6)) { panDir += Vector2.right; }
            if(Input.GetKey(KeyCode.Keypad7)) { panDir += Vector2.up + Vector2.left; }
            if(Input.GetKey(KeyCode.Keypad8)) { panDir += Vector2.up; }
            if(Input.GetKey(KeyCode.Keypad9)) { panDir += Vector2.up + Vector2.right; }
            mapCenter += panDir.normalized * Time.deltaTime * scrollSpeed;
            /// ZOOOOOOOOOMIES ///
            float zoomSpeed = 10f;
            int zoomDir = 0;
            if(Input.GetKey(KeyCode.KeypadMinus)) { zoomDir++; }
            if(Input.GetKey(KeyCode.KeypadPlus)) { zoomDir--; }
            zoom += Time.deltaTime * zoomSpeed * zoomDir;
            zoom = Mathf.Max(10, zoom); // No zooming in further than 10.
            /// Refresh if needed ///
            if(panDir != Vector2.zero || zoomDir != 0) { RefreshMap(); }
        }

        public float GetZoom() => zoom;
        public Vector2 GetCenter() => mapCenter;
        public void DeselectSystem() => SelectSystem(null);

        public async void SelectSystem( SolarSystem sys ) {
            // Deselect
            if(selectedSystem != null && solarSystemObjects.ContainsKey(selectedSystem)) {
                //Nuke the waypoints of the previously selected system.
                foreach(Transform t in solarSystemObjects[selectedSystem].transform.Find("Waypoints")) {
                    GameObject.Destroy(t.gameObject);
                }
            }
            if(sys == null) { return; }

            // Recenter the map.
            mapCenter = new Vector2(selectedSystem.x, selectedSystem.y);
            RefreshMap();

            // Load the waypoints.
            Waypoint wp;
            bool success;
            for(int i = 0; i < sys.waypoints.Count; i++) {
                // Load from cache.
                (success, wp) = await ServerManager.CachedRequest<Waypoint>($"systems/{sys.symbol}/waypoints/{sys.waypoints[i].symbol}", new System.TimeSpan(1, 0, 0), RequestMethod.GET, asyncCancelToken);
                
                if(!success) { continue; } // Skip waypoints that error for some reason.

                // Update the system in memory.
                if(wp != sys.waypoints[i]) {
                    sys.waypoints[i] = wp;
                }
                // Update the system in memory.
                if(wp != sys.waypoints[i]) { sys.waypoints[i] = wp; }
                // Send it.
                SpawnWaypoint(wp, sys, solarSystemObjects[sys]);
            }
            selectedSystem = sys;
        }
        private void RefreshMap() {
            (Vector2 minBounds, Vector2 maxBounds) = GetMapBounds();
            foreach(SolarSystem sys in solarSystems) {
                // Go through each solar system to check the bounds..
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
        // Create the world map as we know it.
        async void CreateMap( int retries = 0 ) {
            bool success;
            (success, solarSystems) = await ServerManager.CachedRequest<List<SolarSystem>>("systems.json", new System.TimeSpan(7, 0, 0, 0), RequestMethod.GET, asyncCancelToken);
            if(!success) {
                Debug.Log("Failed to load systems.json");
                if(retries < 5) {
                    await Task.Delay(1000);
                    CreateMap(retries + 1);
                    return;
                } else {
                    Debug.LogError("5 failed attempts to load systems.json, something is seriously wrong.");
                    return;
                }
            }

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
                    if(asyncCancelToken.IsCancellationRequested) { return; }
                    stopwatch.Start();
                }
            }
            stopwatch.Stop();
            Debug.Log($"Map loaded! {displayedSystems} within range.");
        }
        // Spawn a new known system.
        GameObject SpawnSystem( SolarSystem sys ) {
            GameObject system = Instantiate(SystemPrefab);
            system.transform.parent = SystemContainer;
            SolarSystemVisual sd = system.GetComponent<SolarSystemVisual>();
            sd.system = sys;
            sd.MapManager = this;

            system.SetActive(true);
            return system;
        }
        void SpawnWaypoint( Waypoint waypoint, SolarSystem sys, GameObject system ) {
            GameObject go = GameObject.Instantiate(WaypointPrefab);
            WaypointVisual wpvisual = go.GetComponent<WaypointVisual>();
            go.transform.parent = system.transform.Find("Waypoints");

            wpvisual.waypoint = waypoint;
            wpvisual.solarSystem = sys;
            wpvisual.MapManager = this;

            go.SetActive(true);
        }
        (Vector2, Vector2) GetMapBounds() {
            Vector2 minBounds = new Vector2(zoom * -1 + mapCenter.x, zoom * -1 + mapCenter.y);
            Vector2 maxBounds = new Vector2(zoom + mapCenter.x, zoom + mapCenter.y);
            return (minBounds, maxBounds);
        }
    }
}
