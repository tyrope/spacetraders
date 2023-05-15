using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SpaceTraders
{
    public class MapManager : MonoBehaviour
    {
        public GameObject SystemPrefab;
        public GameObject WaypointPrefab;
        private Transform SystemContainer;
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
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
            CheckInputs();
        }
        private void CheckInputs() {

            if(Input.GetKeyDown(KeyCode.Keypad5)) {
                // Reset map.
                mapCenter = Vector2.zero;
                zoom = 50;
                RefreshMap();
                return;
            }

            /// Scroll the map. ///
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
            zoom = Mathf.Clamp(zoom, 10, 5000);
            RefreshMap();
        }
        private void PanMap(Vector2 delta ) {
            mapCenter += delta;
            RefreshMap();
        }

        public void SelectSystem(SolarSystem sys) {
            if(selectedSystem != null) {
                //Nuke the waypoints of the previously selected system.
                foreach(Transform t in solarSystemObjects[selectedSystem].transform.Find("Waypoints")) {
                    GameObject.Destroy(t.gameObject);
                }
            }
            foreach(Waypoint wp in sys.waypoints) {
                SpawnWaypoint(wp, sys, solarSystemObjects[sys]);
            }
            selectedSystem = sys;
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
            //Get information about the known map.
            solarSystems = ServerManager.Get<List<SolarSystem>>($"/systems.json");
            GameObject go;
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
                if(stopwatch.ElapsedMilliseconds > 1000 / 60) {
                    stopwatch.Stop();
                    stopwatch.Reset();
                    yield return new WaitForEndOfFrame();
                    stopwatch.Start();
                }
            }
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
