using System.Collections.Generic;
using UnityEngine;

namespace SpaceTraders
{
    public class SolarSystem
    {
        public string symbol;
        public string sectorSymbol;
        public string type;
        public int x;
        public int y;
        public List<Waypoint> waypoints;
        public List<object> factions;

        public Vector2 Pos { get => new Vector2(x, y); }

        public override string ToString() {
            return $"System[{symbol}] @ {x}, {y}. Type: {type}. {waypoints.Count} waypoints.";
        }
    }
}