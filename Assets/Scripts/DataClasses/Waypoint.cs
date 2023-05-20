namespace STCommander
{
    public class Waypoint
    {
        public class Orbital
        {
            public string symbol;
            public override string ToString() => symbol;
        }
        public enum WaypointType { PLANET, GAS_GIANT, MOON, ORBITAL_STATION, JUMP_GATE, ASTEROID_FIELD, NEBULA, DEBRIS_FIELD, GRAVITY_WELL }
        public string symbol;
        public WaypointType type;
        public string systemSymbol;
        public int x;
        public int y;
        public Orbital[] orbitals;
        public Faction faction;
        public Trait[] traits;
        public Chart chart;
        public override string ToString() {
            return $"{type} {symbol}@[{x},{y}]Operated by {faction}\nChart:{chart}\nChild bodies:{string.Concat<Orbital>(orbitals)}\nTraits:{string.Concat<Trait>(traits)}";
        }
    }
}
