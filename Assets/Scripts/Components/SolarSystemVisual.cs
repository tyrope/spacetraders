using TMPro;
using UnityEngine;

namespace SpaceTraders
{
    public class SolarSystemVisual : MonoBehaviour
    {
        public MapManager MapManager;
        public SolarSystem system;
        public Material[] materials;

        private Canvas cvs;
        private Vector3 camLocation;

        // Start is called before the first frame update
        void Start() {
            SetPosition();
            gameObject.name = $"[{system.x},{system.y}]{system.symbol}";
            gameObject.GetComponentInChildren<MeshRenderer>().material = materials[(int)system.type];

            cvs = gameObject.GetComponentInChildren<Canvas>();
            cvs.worldCamera = Camera.main;

            gameObject.GetComponentInChildren<TMP_Text>().text = system.symbol;
        }

        // Update is called once per frame
        void Update() {
            if(camLocation == null || camLocation != cvs.worldCamera.transform.position) {
                camLocation = cvs.worldCamera.transform.position;
                cvs.transform.LookAt(cvs.worldCamera.transform);
                cvs.transform.Rotate(new Vector3(0, 1, 0), 180);
            }
        }

        public void SetPosition() {
            float xPos = (system.x + MapManager.GetCenter().x) * 50 / MapManager.GetZoom();
            float yPos = (system.y + MapManager.GetCenter().y) * 50 / MapManager.GetZoom();
            gameObject.transform.position = new Vector3(xPos, 0, yPos);
        }
    }
}
