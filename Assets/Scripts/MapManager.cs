using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SpaceTraders
{
    public class MapManager : MonoBehaviour
    {
        public GameObject SystemPrefab;
        public GameObject WaypointPrefab;
        public GameObject tableLightCone;

        private Transform SystemContainer;
        private List<SolarSystem> solarSystems;
        private readonly Dictionary<SolarSystem, GameObject> solarSystemObjects = new Dictionary<SolarSystem, GameObject>();
        private SolarSystem selectedSystem;

        private Vector2 mapCenter = Vector2.zero;
        private float zoom = 50;
        private int displayedSystems;

        void Start() {
            SystemContainer = new GameObject("SystemContainer").transform;
            SystemContainer.position = Vector3.zero;
            SystemContainer.parent = gameObject.transform.parent;

            StartCoroutine(CreateMap());
        }

        void Update() {
            MapControls();

            // TODO Deselect using in-world stuff, not rightclick.
            if(Input.GetKeyDown(KeyCode.Mouse1)) { DeselectSystem(); }
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

            if(Input.GetKey(KeyCode.Keypad1)) panDir += Vector2.up + Vector2.right;
            if(Input.GetKey(KeyCode.Keypad2)) panDir += Vector2.up;
            if(Input.GetKey(KeyCode.Keypad3)) panDir += Vector2.up + Vector2.left;
            if(Input.GetKey(KeyCode.Keypad4)) panDir += Vector2.right;
            if(Input.GetKey(KeyCode.Keypad6)) panDir += Vector2.left;
            if(Input.GetKey(KeyCode.Keypad7)) panDir += Vector2.down + Vector2.right;
            if(Input.GetKey(KeyCode.Keypad8)) panDir += Vector2.down;
            if(Input.GetKey(KeyCode.Keypad9)) panDir += Vector2.down + Vector2.left;
        
            if(panDir != Vector2.zero) {
                PanMap(panDir.normalized * Time.deltaTime * scrollSpeed);
            }

            /// ZOOOOOOOOOMIES ///
            float zoomSpeed = 10f;
            int zoomDir = 0;
            if(Input.GetKey(KeyCode.KeypadMinus)) zoomDir++;
            if(Input.GetKey(KeyCode.KeypadPlus)) zoomDir--;

            if(zoomDir != 0) {
                ChangeZoom(Time.deltaTime * zoomSpeed * zoomDir);
            }
        }

        public float GetZoom() => zoom;
        public Vector2 GetCenter() => mapCenter;
        private void ChangeZoom( float delta ) {
            zoom += delta;
            zoom = Mathf.Max(10, zoom); // No zooming in further than 10.
            RefreshMap();
        }
        private void PanMap(Vector2 delta ) {
            mapCenter += delta;
            RefreshMap();
        }

        public void DeselectSystem() {
            SelectSystem(null);
        }

        public void SelectSystem(SolarSystem sys) {
            if(selectedSystem != null && solarSystemObjects.ContainsKey(selectedSystem)) {
                //Nuke the waypoints of the previously selected system.
                foreach(Transform t in solarSystemObjects[selectedSystem].transform.Find("Waypoints")) {
                    GameObject.Destroy(t.gameObject);
                }
            }
            if(sys == null) {
                // We're deselecting.
                tableLightCone.SetActive(false);
                return;
            }

            tableLightCone.SetActive(true);
            foreach(Waypoint wp in sys.waypoints) {
                SpawnWaypoint(wp, sys, solarSystemObjects[sys]);
            }
            selectedSystem = sys;
            Vector2 newCenter = new Vector2(selectedSystem.x, selectedSystem.y) - mapCenter * -1;
            PanMap(newCenter);
        }

        private void RefreshMap() {
            Vector2 minBounds = new Vector2(zoom * -1 - mapCenter.x, zoom * -1 - mapCenter.y);
            Vector2 maxBounds = new Vector2(zoom - mapCenter.x, zoom - mapCenter.y);
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
        IEnumerator CreateMap() {
            solarSystems = ServerManager.CachedRequest<List<SolarSystem>>("systems.json", new System.TimeSpan(7, 0, 0, 0), RequestMethod.GET);

            GameObject go;
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Reset();
            stopwatch.Start();
            foreach(SolarSystem sys in solarSystems) {
                if(sys.x < zoom * -1 + mapCenter.x || sys.x > zoom + mapCenter.x || sys.y < zoom * -1 + mapCenter.y || sys.y > zoom + mapCenter.y) {
                    // Skip out-of-bounds systems.
                    continue;
                }

                displayedSystems++;
                go = SpawnSystem(sys);
                solarSystemObjects.Add(sys, go);
                if(stopwatch.ElapsedMilliseconds > 16.666f) { // 1000 / 60, rounded down.
                    stopwatch.Stop();
                    stopwatch.Reset();
                    yield return new WaitForEndOfFrame();
                    stopwatch.Start();
                }
            }
            stopwatch.Stop();
            Debug.Log($"Map loaded! {displayedSystems} within range.");
        }

        // Spawn a new known system.
        GameObject SpawnSystem( SolarSystem sys ) {
            GameObject system = GameObject.Instantiate(SystemPrefab);
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
    }
}
