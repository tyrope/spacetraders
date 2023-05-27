using System;
using UnityEngine;

namespace STCommander
{
    public abstract class OrbitalVisual : MonoBehaviour
    {
        public MapManager mapManager;
        protected float OrbitTime = 0f;
        protected float OrbitalAltitude;
        protected virtual float OrbitalPeriod => Mathf.Sqrt(4 * Mathf.Pow(Mathf.PI, 2) * Mathf.Pow(OrbitalAltitude * mapManager.GetMapScale(), 3) / 6.67430e-11f) / 10000f;
        protected float OrbitalAngle => OrbitTime / OrbitalPeriod * 360f;
        protected Vector3 OrbitalPosition => new Vector3(Mathf.Sin(OrbitalAngle* Mathf.Deg2Rad), 0, Mathf.Cos(OrbitalAngle* Mathf.Deg2Rad)) * OrbitalAltitude * mapManager.GetMapScale();

        protected virtual void Start() {
            float nowInSeconds = (float) DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds;
            OrbitTime = nowInSeconds % OrbitalPeriod;
        }
        protected virtual void Update() {
            OrbitTime += Time.deltaTime;
            OrbitTime %= OrbitalPeriod;
        }
    }
}
