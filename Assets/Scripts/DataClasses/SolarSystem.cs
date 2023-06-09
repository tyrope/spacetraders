using System.Collections.Generic;

namespace STCommander
{
    public class SolarSystem
    {
        public readonly static Dictionary<string, SolarSystem> Instances = new Dictionary<string, SolarSystem>();

        public enum StarType { NEUTRON_STAR, RED_STAR, ORANGE_STAR, BLUE_STAR, YOUNG_STAR, WHITE_DWARF, BLACK_HOLE, HYPERGIANT, NEBULA, UNSTABLE };
        public string symbol;
        public string sectorSymbol;
        public StarType type;
        public int x;
        public int y;
        public List<string> waypoints;
        public List<string> factions;
        public override string ToString() {
            return $"System[{symbol}] @ {x}, {y}. Type: {type}. {waypoints.Count} waypoints.";
        }
    }
}
