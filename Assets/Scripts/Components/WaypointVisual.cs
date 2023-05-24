using System;
using TMPro;
using UnityEngine;

namespace STCommander
{
    public class WaypointVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem solarSystem;
        public Waypoint waypoint;
        public GameObject[] models;

        private Transform parentOrbit = null;
        private int totalSatellites = 1;
        private int ourSatelliteIndex = 0;
        private string WaypointSymbolEnd => waypoint.symbol.Split('-')[2];

        private float OrbitalAltitude;
        private float OrbitalPeriod => Mathf.Sqrt((4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(parentOrbit == null ? OrbitalAltitude * solarSystem.GetSystemScale() : OrbitalAltitude, 3)) / 6.67430e-11f) / 1000f;
        private float OrbitTime;

        // Start is called before the first frame update
        void Start() {
            Instantiate(models[(int) waypoint.type], transform.Find("Visuals"));
            gameObject.name = $"({waypoint.x},{waypoint.y}){WaypointSymbolEnd}";
            gameObject.GetComponentInChildren<TMP_Text>().text = WaypointSymbolEnd;

            // Are we orbiting the main star, or another waypoint?
            SetParentOrbit();
            if(parentOrbit == null) {
                OrbitalAltitude = new Vector2(waypoint.x, waypoint.y).magnitude;
                OrbitTime = DateTime.Now.Ticks % OrbitalPeriod;
            } else {
                OrbitalAltitude = 2f;
                // TODO Fix all satellites overlapping despite being offset by Index.
                OrbitTime = DateTime.Now.Ticks + OrbitalPeriod * (ourSatelliteIndex / (float) totalSatellites) % OrbitalPeriod;
            }
            SetPosition();
        }

        private void SetParentOrbit() {
            foreach(Waypoint wp in solarSystem.waypoints) {
                if(wp.orbitals != null) {
                    for(int i = 0; i < wp.orbitals.Length; i++) {
                        if(wp.orbitals[i].symbol == waypoint.symbol) {
                            parentOrbit = transform.parent.Find($"({wp.x},{wp.y}){wp.symbol.Split('-')[2]}");
                            totalSatellites = wp.orbitals.Length;
                            ourSatelliteIndex = i;
                            return;
                        }
                    }
                }
            }
        }

        private void Update() {
            OrbitTime += Time.deltaTime;
            if(OrbitTime >= OrbitalPeriod) { OrbitTime -= OrbitalPeriod; }
            SetPosition();
        }

        public void SetPosition() {
            float rot = (OrbitTime / OrbitalPeriod) * 360f;
            if(parentOrbit == null) {
                // We're orbiting the main star; align around 0.
                transform.position = new Vector3(
                    OrbitalAltitude * solarSystem.GetSystemScale() * Mathf.Sin(rot), 0,
                    OrbitalAltitude * solarSystem.GetSystemScale() * Mathf.Cos(rot));
            } else {
                // We're orbiting another body; align around them.
                // TODO These don't actually orbit, they're just static relative to their parent.
                transform.position = parentOrbit.position +
                    new Vector3(
                        OrbitalAltitude * solarSystem.GetSystemScale() * Mathf.Sin(rot), 0,
                        OrbitalAltitude * solarSystem.GetSystemScale() * Mathf.Cos(rot));
            }
        }
    }
}
