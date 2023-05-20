using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STCommander
{
    public class InputController : MonoBehaviour
    {
        public CameraController camController;
        public ConsoleController consoleController;
        public GameObject HintsWindow;
        public GameObject ShipsWindow;
        public GameObject ContractsWindow;

        // Update is called once per frame
        void Update() {
            if(Input.GetKeyDown(KeyCode.F1)) 
                HintsWindow.SetActive(HintsWindow.activeSelf == false);

            if(Input.GetKeyDown(KeyCode.F2))
                ShipsWindow.SetActive(ShipsWindow.activeSelf == false);

            if(Input.GetKeyDown(KeyCode.F3))
                ContractsWindow.SetActive(ContractsWindow.activeSelf == false);

            if(consoleController.ParseInputs())
                camController.ParseInputs();
        }
    }
}
