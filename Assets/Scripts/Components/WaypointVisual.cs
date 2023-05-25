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
        private int SatelliteIndex = 0;
        private string WaypointSymbolEnd => waypoint.symbol.Split('-')[2];

        private float OrbitalAltitude;
        private float OrbitalPeriod => Mathf.Sqrt((4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(OrbitalAltitude * solarSystem.GetSystemScale(), 3)) / 6.67430e-11f) / (parentOrbit == null ? 500f : 250f);
        private float OrbitTime;

        // Start is called before the first frame update
        void Start() {
            Instantiate(models[(int) waypoint.type], transform.Find("Visuals"));
            gameObject.name = $"({waypoint.x},{waypoint.y}){WaypointSymbolEnd}";
            gameObject.GetComponentInChildren<TMP_Text>().text = WaypointSymbolEnd;

            // Are we orbiting the main star, or another waypoint?
            SetParentOrbit();

            float nowInSeconds = (float) DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds;

            if(parentOrbit == null) {
                OrbitalAltitude = new Vector2(waypoint.x, waypoint.y).magnitude;
            } else {
                OrbitalAltitude = 3f + SatelliteIndex;
            }
            OrbitTime = nowInSeconds % OrbitalPeriod;
            SetPosition();
        }

        private void SetParentOrbit() {
            foreach(Waypoint wp in solarSystem.waypoints) {
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
        }

        private void Update() {
            OrbitTime += Time.deltaTime;
            if(OrbitTime >= OrbitalPeriod) { OrbitTime %= OrbitalPeriod; }
            SetPosition();
        }

        public void SetPosition() {
            float rot = (OrbitTime / OrbitalPeriod) * 360f;
            float scaledAltitude = OrbitalAltitude * solarSystem.GetSystemScale();
            if(parentOrbit == null) {
                // We're orbiting the main star; align around 0.
                transform.position = new Vector3(scaledAltitude * Mathf.Sin(rot), 0, scaledAltitude * Mathf.Cos(rot));
            } else {
                // We're orbiting another body; align around it.
                transform.position = parentOrbit.position +
                    new Vector3(scaledAltitude * Mathf.Sin(rot), 0, scaledAltitude * Mathf.Cos(rot));
            }
        }
    }
}
