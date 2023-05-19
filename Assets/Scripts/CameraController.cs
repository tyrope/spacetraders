using UnityEngine;

namespace SpaceTraders
{
    public class CameraController : MonoBehaviour {

        public float mouseSensitivity = 4f;
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        private Transform camTransform;

        private bool mouseReleased = false;

        // Start is called before the first frame update
        void Start() {
            camTransform = Camera.main.transform;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Update is called once per frame
        void Update() {
            if(Input.GetKeyDown(KeyCode.LeftAlt)) {
                // Toggle mouse release status
                mouseReleased = !mouseReleased;

                // Toggle the cursor.
                Cursor.lockState = mouseReleased ? CursorLockMode.None : CursorLockMode.Locked;
            }

            // We're in UI mode.
            if(mouseReleased) return;
            MovePlayer();
            RotateCamera();
        }

        private void MovePlayer() {
            // Set speed to running if we're holding shift, or walking otherwise.
            float moveSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? runSpeed : walkSpeed;
            Vector3 moveDirection = Vector3.zero;

            if(Input.GetKey(KeyCode.W)) {
                moveDirection += Vector3.forward;
            }
            if(Input.GetKey(KeyCode.A)) {
                moveDirection += Vector3.left;
            }
            if(Input.GetKey(KeyCode.S)) {
                moveDirection += Vector3.back;
            }
            if(Input.GetKey(KeyCode.D)) {
                moveDirection += Vector3.right;
            }
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        }

        private float cameraRotationY;
        private void RotateCamera() {
            // get the mouse inputs
            float y = Input.GetAxis("Mouse X") * mouseSensitivity;
            cameraRotationY += Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Let's not look upside-down.
            cameraRotationY = Mathf.Clamp(cameraRotationY, -90f, 90f);

            // Send it.
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y + y, 0);
            camTransform.eulerAngles = new Vector3(-cameraRotationY, transform.eulerAngles.y + y, 0);
        }
    }
}