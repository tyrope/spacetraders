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

        private float OrbitalAltitude;
        private float OrbitalPeriod => Mathf.Sqrt((4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(OrbitalAltitude, 3)) / 6.67430e-11f)/1000f;
        private float OrbitTime;

        // Start is called before the first frame update
        void Start() {
            Instantiate(models[(int) waypoint.type], transform.Find("Visuals"));
            gameObject.name = $"({waypoint.x},{waypoint.y}){waypoint.symbol.Split('-')[2]}";
            gameObject.GetComponentInChildren<TMP_Text>().text = waypoint.symbol.Split('-')[2];
            OrbitalAltitude = new Vector2(waypoint.x, waypoint.y).magnitude * solarSystem.GetSystemScale();

            OrbitTime = System.DateTime.Now.Ticks % OrbitalPeriod;
            SetPosition();
        }

        private void Update() {
            OrbitTime += Time.deltaTime;
            if(OrbitTime >= OrbitalPeriod) { OrbitTime -= OrbitalPeriod; }
            SetPosition();
        }

        public void SetPosition() {
            float rotation = (OrbitTime / OrbitalPeriod) * 360f;
            float xPos = OrbitalAltitude * Mathf.Sin(rotation);
            float yPos = OrbitalAltitude * Mathf.Cos(rotation);
            transform.position = new Vector3(xPos, 0, yPos);
        }
    }
}
