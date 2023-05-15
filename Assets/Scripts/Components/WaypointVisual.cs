using TMPro;
using UnityEngine;

namespace SpaceTraders
{
    public class WaypointVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem solarSystem;
        public Waypoint waypoint;

        private Canvas cvs;
        // Start is called before the first frame update
        void Start() {
            SetPosition();
            gameObject.name = $"[{waypoint.x},{waypoint.y}]{waypoint.symbol.Split('-')[2]}";

            cvs = gameObject.GetComponentInChildren<Canvas>();
            cvs.worldCamera = Camera.main;

            gameObject.GetComponentInChildren<TMP_Text>().text = waypoint.symbol.Split('-')[2];
        }

        // Update is called once per frame
        void Update() {
            cvs.transform.LookAt(Camera.main.transform);
            cvs.transform.Rotate(new Vector3(0, 1, 0), 180);
        }

        public void SetPosition() {
            float xPos = waypoint.x * MapManager.GetZoom() / 1000;
            float yPos = waypoint.y * MapManager.GetZoom() / 1000;
            Vector3 subPos = new Vector3(xPos, 0, yPos);
            Vector3 parentPos = gameObject.transform.parent.position;
            Vector3 newPos = parentPos + subPos;
            gameObject.transform.position = newPos;
        }
    }
}
