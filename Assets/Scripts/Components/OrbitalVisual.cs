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
        protected Vector3 OrbitalPosition => new Vector3(OrbitalAltitude * mapManager.GetMapScale() * Mathf.Sin(OrbitalAngle), 0, OrbitalAltitude * mapManager.GetMapScale() * Mathf.Cos(OrbitalAngle));

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