using UnityEngine;

namespace STCommander
{
    public class ShipVisual : MonoBehaviour
    {
        public Ship ship;
        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            if(ship.nav.status == Ship.Navigation.Status.IN_ORBIT) {
                transform.Rotate(Vector3.up, Time.deltaTime);
            }
        }
    }
}
