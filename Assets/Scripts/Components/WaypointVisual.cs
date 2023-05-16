using TMPro;
using UnityEngine;

namespace SpaceTraders
{
    public class WaypointVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem solarSystem;
        public Waypoint waypoint;
        public GameObject[] models;

        // Start is called before the first frame update
        void Start() {
            Instantiate(models[(int) waypoint.type], transform.Find("Visuals"));
            gameObject.name = $"[{waypoint.x},{waypoint.y}]{waypoint.symbol.Split('-')[2]}";
            gameObject.GetComponentInChildren<TMP_Text>().text = waypoint.symbol.Split('-')[2];
            SetPosition();
        }

        public void SetPosition() {
            float xPos = waypoint.x / 88f; // Scale from -200 < x <  200
            float yPos = waypoint.y / 88f; //    to      -2.2727 < x < 2.2727
            gameObject.transform.position = new Vector3(xPos, 0, yPos);
        }
    }
}
