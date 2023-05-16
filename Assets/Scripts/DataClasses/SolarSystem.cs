using System.Collections.Generic;

namespace SpaceTraders
{
    public class SolarSystem {
        public enum StarType { NEUTRON_STAR, RED_STAR, ORANGE_STAR, BLUE_STAR, YOUNG_STAR, WHITE_DWARF, BLACK_HOLE, HYPERGIANT, NEBULA, UNSTABLE };
        public string symbol;
        public string sectorSymbol;
        public StarType type;
        public int x;
        public int y;
        public List<Waypoint> waypoints;
        public List<object> factions;

        public override string ToString() {
            return $"System[{symbol}] @ {x}, {y}. Type: {type}. {waypoints.Count} waypoints.";
        }
    }
}