using UnityEngine;

namespace STCommander
{
    public class InputController : MonoBehaviour
    {
        private CameraController camController;
        private ConsoleController consoleController;
        private MapManager mapManager;

        public GameObject HintsWindow;
        public GameObject ShipsWindow;
        public GameObject ContractsWindow;

        private void Start() {
            camController = gameObject.GetComponent<CameraController>();
            consoleController = gameObject.GetComponent<ConsoleController>();
            mapManager = gameObject.GetComponent<MapManager>();
        }

        // Update is called once per frame
        void Update() {
            if(Input.GetKeyDown(KeyCode.F1)) { HintsWindow.SetActive(HintsWindow.activeSelf == false); }
            if(Input.GetKeyDown(KeyCode.F2)) { ShipsWindow.SetActive(ShipsWindow.activeSelf == false); }
            if(Input.GetKeyDown(KeyCode.F3)) { ContractsWindow.SetActive(ContractsWindow.activeSelf == false); }

            if(consoleController.ParseInputs()) {
                camController.ParseInputs();
                mapManager.ParseInputs();
            }
        }
    }
}
