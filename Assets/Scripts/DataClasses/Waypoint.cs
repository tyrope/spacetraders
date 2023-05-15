using UnityEngine;

namespace SpaceTraders
{
    public class Waypoint
    {
        public string symbol;
        public string type;
        public int x;
        public int y;
        public Vector2 Pos { get => new Vector2(x, y); }

        public override string ToString() {
            return $"Waypoint[{symbol}] @ {x}, {y}. Type: {type}";
        }
    }
}