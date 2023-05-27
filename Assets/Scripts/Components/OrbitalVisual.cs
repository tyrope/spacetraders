using System;
using UnityEngine;

namespace STCommander
{
    public abstract class OrbitalVisual : MonoBehaviour
    {
        public MapManager mapManager;
        protected float OrbitTime = 0f;
        protected float OrbitalAltitude;
        protected virtual float OrbitalPeriod => Mathf.Sqrt(4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(OrbitalAltitude * mapManager.GetMapScale(), 3) / 6.67430e-11f) / 500f;
        protected float OrbitalAngle => OrbitTime / OrbitalPeriod * 360f;
        protected Vector3 OrbitalPosition => new Vector3(Mathf.Sin(OrbitalAngle), 0, Mathf.Cos(OrbitalAngle)) * OrbitalAltitude * mapManager.GetMapScale();

        private bool printedThisOrbit = false; //DEBUG

        protected virtual void Start() {
            float nowInSeconds = (float) DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds;
            OrbitTime = nowInSeconds % OrbitalPeriod;
        }
        protected virtual void Update() {
            OrbitTime += Time.deltaTime;
            OrbitTime %= OrbitalPeriod;

            if(OrbitalPosition.z < 0 && Mathf.Abs(OrbitalPosition.x) < 0.01f && printedThisOrbit == false) {
                printedThisOrbit = true;
                Debug.Log($"{transform.name} reached the top of the orbit ({OrbitalAngle}°) at {Mathf.RoundToInt(OrbitTime)}/{Mathf.RoundToInt(OrbitalPeriod)}");
                /* TYR0PE-1 reached the top of the orbit (9.391718°) at 7/253
                 * TYR0PE-1 reached the top of the orbit (15.67945°) at 11/253
                 * TYR0PE-1 reached the top of the orbit (21.96634°) at 15/253
                 * TYR0PE-1 reached the top of the orbit (28.24255°) at 20/253
                */
            } else if(OrbitalPosition.z > 0) {
                printedThisOrbit = false;
            }
        }
    }
}
