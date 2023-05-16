namespace SpaceTraders
{
    public class Waypoint
    {
        public enum WaypointType { PLANET, GAS_GIANT, MOON, ORBITAL_STATION, JUMP_GATE, ASTEROID_FIELD, NEBULA, DEBRIS_FIELD, GRAVITY_WELL }
        public string symbol;
        public WaypointType type;
        public int x;
        public int y;
        public string[] orbitals;
        public string faction;
        public Trait[] traits;
        public string[] chart;

        public override string ToString() {
            return $"Waypoint[{symbol}] @ {x}, {y}. Type: {type}";
        }
    }
}