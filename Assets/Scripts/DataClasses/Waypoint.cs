namespace SpaceTraders
{
    public class Waypoint
    {
        public string symbol;
        public string type;
        public int x;
        public int y;

        public override string ToString() {
            return $"Waypoint[{symbol}] @ {x}, {y}. Type: {type}";
        }
    }
}