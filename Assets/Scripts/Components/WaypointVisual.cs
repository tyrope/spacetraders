using System;
using TMPro;
using UnityEngine;

namespace STCommander
{
    public class WaypointVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public Waypoint waypoint;
        public GameObject[] models;

        private Transform parentOrbit = null;
        private int SatelliteIndex = 0;
        private string WaypointSymbolEnd => waypoint.symbol.Split('-')[2];

        private float OrbitalAltitude;
        private float OrbitTime;

        // Start is called before the first frame update
        void Start() {
            Instantiate(models[(int) waypoint.type], transform.Find("Visuals"));
            gameObject.name = $"({waypoint.x},{waypoint.y}){WaypointSymbolEnd}";
            gameObject.GetComponentInChildren<TMP_Text>().text = WaypointSymbolEnd;

            // Are we orbiting the selected object, or another waypoint?
            SetParentOrbit();

            float nowInSeconds = (float) DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds;

            if(parentOrbit == null) {
                OrbitalAltitude = new Vector2(waypoint.x, waypoint.y).magnitude;
            } else {
                OrbitalAltitude = 3f + SatelliteIndex;
            }
            OrbitTime = nowInSeconds % GetOrbitalPeriod();
            SetPosition();
        }

        void OnMouseDown() {
            Debug.Log("Selected waypoint: " + waypoint.symbol);
            MapManager.SelectWaypoint(waypoint);
        }

        private void Update() {
            OrbitTime += Time.deltaTime;
            if(OrbitTime >= GetOrbitalPeriod()) { OrbitTime %= GetOrbitalPeriod(); }
            SetPosition();
        }

        private void SetParentOrbit() {
            if(MapManager.SelectedSystem != null) {
                // Main star is the middle point.
                foreach(Waypoint wp in MapManager.SelectedSystem.waypoints) {
                    if(wp.orbitals != null) {
                        for(int i = 0; i < wp.orbitals.Length; i++) {
                            if(wp.orbitals[i].symbol == waypoint.symbol) {
                                parentOrbit = transform.parent.Find($"({wp.x},{wp.y}){wp.symbol.Split('-')[2]}");
                                SatelliteIndex = i;
                                return;
                            }
                        }
                    }
                }
            } else {
                // A waypoint is the middle point.
                if(MapManager.SelectedWaypoint == waypoint) {
                    return; // We are the middle point.
                }
                Waypoint.Orbital o;
                for(int i = 0; i < MapManager.SelectedWaypoint.orbitals.Length; i++) {
                    o = MapManager.SelectedWaypoint.orbitals[i];
                    if(o.symbol == waypoint.symbol) {
                        parentOrbit = transform.parent.Find($"({waypoint.x},{waypoint.y}){MapManager.SelectedWaypoint.symbol.Split('-')[2]}");
                        SatelliteIndex = i;
                        break;
                    }
                }
            }
        }

        private void SetPosition() {
            if(MapManager.SelectedWaypoint == waypoint) {
                return; // We're the middle point; don't orbit.
            }
            float rot = (OrbitTime / GetOrbitalPeriod()) * 360f;
            float scaledAltitude = OrbitalAltitude * MapManager.GetMapScale();
            if(parentOrbit == null) {
                // We're orbiting the main star; align around 0.
                transform.position = new Vector3(scaledAltitude * Mathf.Sin(rot), 0, scaledAltitude * Mathf.Cos(rot));
            } else {
                // We're orbiting another body; align around it.
                transform.position = parentOrbit.position +
                    new Vector3(scaledAltitude * Mathf.Sin(rot), 0, scaledAltitude * Mathf.Cos(rot));
            }
        }
        private float GetOrbitalPeriod() {
            return Mathf.Sqrt(4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(OrbitalAltitude * MapManager.GetMapScale(), 3) / 6.67430e-11f) / (parentOrbit == null ? 500f : 250f);
        }
    }
}
