using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STCommander
{
    public class InputController : MonoBehaviour
    {
        public CameraController camController;
        public ConsoleController consoleController;

        // Update is called once per frame
        void Update() {
            if(consoleController.ParseInputs()) {
                camController.ParseInputs();
            }
        }
    }
}
