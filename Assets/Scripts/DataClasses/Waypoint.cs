using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace STCommander
{
    public class Waypoint : IDataClass
    {
        public readonly static Dictionary<string, Waypoint> Instances = new Dictionary<string, Waypoint>();

        public class Chart
        {
            public string waypointSymbol;
            public string submittedBy;
            public DateTime subtmittedOn;
        }
        public enum WaypointType { PLANET, GAS_GIANT, MOON, ORBITAL_STATION, JUMP_GATE, ASTEROID_FIELD, NEBULA, DEBRIS_FIELD, GRAVITY_WELL }
        public string symbol;
        public WaypointType type;
        public string systemSymbol;
        public int x;
        public int y;
        public string[] orbitals;
        public string faction;
        public Trait[] traits;
        public Chart chart;

        public override string ToString() {
            string retString = $"{type} {symbol}@[{x},{y}]";
            if(faction != null) {
                retString += $"\nOperated by {faction}";
            }
            if(chart != null) {
                retString += $"\nChart:{chart}";
            }
            if(orbitals != null) {
                retString += $"\nChild bodies:{string.Join(", ", orbitals)}";
            }
            if(traits != null) {
                retString += $"\nTraits:{string.Concat<Trait>(traits)}";
            }

            return retString;
        }

        public Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge, CancellationToken cancel ) {
            throw new NotImplementedException();
        }

        public Task<bool> SaveToCache( CancellationToken cancel ) {
            throw new NotImplementedException();
        }
    }
}
