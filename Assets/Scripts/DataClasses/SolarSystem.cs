using System.Collections.Generic;
using UnityEngine;

namespace STCommander
{
    public class SolarSystem
    {
        public enum StarType { NEUTRON_STAR, RED_STAR, ORANGE_STAR, BLUE_STAR, YOUNG_STAR, WHITE_DWARF, BLACK_HOLE, HYPERGIANT, NEBULA, UNSTABLE };
        public string symbol;
        public string sectorSymbol;
        public StarType type;
        public int x;
        public int y;
        public List<Waypoint> waypoints;
        public List<Faction> factions;
        public override string ToString() {
            return $"System[{symbol}] @ {x}, {y}. Type: {type}. {waypoints.Count} waypoints.";
        }
        public float GetSystemScale() {
            float maxMagnitude = 0f;
            foreach(Waypoint wp in waypoints) {
                float mag = new Vector2(wp.x, wp.y).magnitude;
                if(mag > maxMagnitude) { maxMagnitude = mag; }
            }
            return 2.0f / maxMagnitude;
        }
    }
}
