using TMPro;
using UnityEngine;

namespace SpaceTraders
{
    public class WaypointVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem solarSystem;
        public Waypoint waypoint;

        // Start is called before the first frame update
        void Start() {
            SetPosition();
            gameObject.name = $"[{waypoint.x},{waypoint.y}]{waypoint.symbol.Split('-')[2]}";

            gameObject.GetComponentInChildren<TMP_Text>().text = waypoint.symbol;
        }

        public void SetPosition() {
            float xPos = waypoint.x / 88f; // Scale from -200 < x <  200
            float yPos = waypoint.y / 88f; //    to      -2.2727 < x < 2.2727
            gameObject.transform.position = new Vector3(xPos, 0, yPos);
        }
    }
}
