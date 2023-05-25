using TMPro;
using UnityEngine;

namespace STCommander
{
    public class SolarSystemVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem system;
        public GameObject[] models;

        // Start is called before the first frame update
        private void Start() {
            Instantiate(models[(int) system.type], transform.Find("Visuals"));
            gameObject.name = $"({system.x},{system.y}){system.symbol}";
            gameObject.GetComponentInChildren<TMP_Text>().text = system.symbol;
            SetPosition();
        }

        void OnMouseDown() {
            Debug.Log("Selected system: " + system.symbol);
            MapManager.SelectSystem(system);
        }

        public void SetPosition() {
            if(MapManager.SelectedSystem == system) {
                gameObject.transform.position = new Vector3(0, 0.5f, 0);
                return;
            }
            Vector2 mapCenter = MapManager.GetCenter() * -1;
            float xPos = system.x + mapCenter.x;
            float yPos = system.y + mapCenter.y;
            gameObject.transform.position = new Vector3(xPos, 0, yPos) / MapManager.GetZoom() * 2f;
        }
    }
}
