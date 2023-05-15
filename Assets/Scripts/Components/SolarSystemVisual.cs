using TMPro;
using UnityEngine;

namespace SpaceTraders
{
    public class SolarSystemVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem system;
        public Material[] materials;

        // Start is called before the first frame update
        void Start() {
            SetPosition();
            gameObject.name = $"[{system.x},{system.y}]{system.symbol}";
            gameObject.GetComponentInChildren<MeshRenderer>().material = materials[(int)system.type];

            gameObject.GetComponentInChildren<TMP_Text>().text = system.symbol;
        }

        void OnMouseDown() {
            MapManager.SelectSystem(system);
        }

        public void SetPosition() {
            float xPos = system.x + MapManager.GetCenter().x;
            float yPos = system.y + MapManager.GetCenter().y;
            gameObject.transform.position = new Vector3(xPos, 0, yPos) / MapManager.GetZoom() * 2f;
        }
    }
}
