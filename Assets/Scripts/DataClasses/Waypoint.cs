using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class Waypoint : IDataClass
    {
        public readonly static Dictionary<string, Waypoint> Instances = new Dictionary<string, Waypoint>();

        public class Chart
        {
            public string submittedBy;
            public DateTime submittedOn;
            public double Timestamp => (submittedOn - DateTime.UnixEpoch).TotalSeconds;
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

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge, CancellationToken cancel ) {
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            string waypointSymbol = endpoint.Trim('/').Split('/')[^1];

            List<List<object>> waypoints = await DatabaseManager.instance.SelectQuery(
                $"SELECT type, systemSymbol, x, y, faction, submittedBy, submittedOn FROM Waypoint LEFT JOIN Chart ON Waypoint.symbol=Chart.waypointSymbol" +
                $"WHERE Waypoint.lastEdited<{highestUnixTimestamp} AND Chart.lastEdited<{highestUnixTimestamp} AND symbol='{waypointSymbol}'",
                cancel);
            if(cancel.IsCancellationRequested) { return default; }
            if(waypoints.Count == 0) {
                Debug.Log($"Waypoint::LoadFromCache() -- No results.");
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<List<object>> traits;
            List<List<object>> orbitals;
            foreach(List<object> wp in waypoints) {
                traits = await DatabaseManager.instance.SelectQuery(
                    $"SELECT symbol, name, description FROM WaypointTrait LEFT JOIN Waypoint_WaypointTrait_relationship Rel ON Rel.trait=WaypointTrait.symbol WHERE Rel.waypoint='{waypointSymbol}';", cancel);
                if(cancel.IsCancellationRequested) { return default; }
                orbitals = await DatabaseManager.instance.SelectQuery($"SELECT symbol FROM Orbital WHERE parent='{waypointSymbol}';", cancel);
                if(cancel.IsCancellationRequested) { return default; }
                if(Instances.ContainsKey((string) wp[0])) {
                    ret.Add(Instances[(string) wp[0]].Update(wp, traits, orbitals));
                } else {
                    ret.Add(new Waypoint(waypointSymbol, wp, traits, orbitals));
                }
            }
            return ret;
        }

        private Waypoint Update(List<object>prms, List<List<object>> trts, List<List<object>> orbs) {
            type = Enum.Parse<WaypointType>((string) prms[0]);
            systemSymbol = (string) prms[1];
            x = (int) prms[2];
            y = (int) prms[3];
            faction = (string) prms[4];
            chart = new Chart() { submittedBy = (string) prms[5], submittedOn = (DateTime) prms[6] };

            List<string> os = new List<string>();
            foreach(List<object> orbital in orbs) {
                os.Add((string) orbital[0]);
            }
            orbitals = os.ToArray();

            List<Trait> ts = new List<Trait>();
            foreach(List<string> trait in trts.Cast<List<string>>()) {
                ts.Add(Trait.GetTrait(trait[0], trait[1], trait[2]));
            }
            traits = ts.ToArray();
            return this;
        }

        private Waypoint( string smbl, List<object> prms, List<List<object>> trts, List<List<object>> orbs ) {
            symbol = smbl;
            Update(prms, trts, orbs);
        }

        public async Task<bool> SaveToCache( CancellationToken cancel ) {
            string query = "BEGIN TRANSACTION;\n"; // This is going to be a big update. Do not let anybody else interfere.

            // Chart: waypointSymbol (TEXT NOT NULL), submittedBy (TEXT), submittedOn (INT), lastEdited (INT NOT NULL)
            query += "INSERT INTO Chart (waypointSymbol, submittedBy, submittedOn, lastEdited) VALUES (" +
                $"{symbol}, '{chart.submittedBy}', '{chart.Timestamp}',STRFTIME('%s')" +
                ") ON CONFLICT(symbol) DO UPDATE SET submittedBy=excluded.submittedBy,submittedOn=excluded.submittedOn,lastEdited=excluded.lastEdited;\n";


            // WaypointTrait: 	"symbol"	TEXT NOT NULL,            "name"  TEXT NOT NULL,	"description"  (TEXT NOT NULL)
            query += "INSERT OR IGNORE INTO WaypointTrait (symbol, name, description) VALUES ";
            foreach(Trait t in traits) {
                query += $"('{t.symbol}','{t.name}','{t.description}'),";
            }
            query = query[0..^1] + ";\n";


            // Waypoint_WaypointTrait_relationship: waypoint (TEXT NOT NULL), trait (TEXT NOT NULL)
            query += "INSERT OR IGNORE INTO Waypoint_WaypointTrait_relationship (waypoint, trait) VALUES ";
            foreach(Trait t in traits) {
                query += $"('{symbol}','{t.symbol}'),";
            }
            query = query[0..^1] + ";\n";


            // Waypoint: symbol (TEXT NOT NULL), type (TEXT NOT NULL), systemSymbol (TEXT NOT NULL), x (INT NOT NULL), y (INT NOT NULL), faction (TEXT), lastEdited (INT NOT NULL)
            query += $"INSERT INTO Waypoint (symbol, type, systemSymbol, x, y, faction, lastEdited) VALUES ('{symbol}','{type}','{systemSymbol}',{x},{y},'{faction}',STRFTIME('%s')) ";
            query += "ON CONFLICT(symbol) DO UPDATE SET faction=excluded.faction,lastEdited=excluded.lastEdited;\n";


            query += "COMMIT;"; // Send it!
            return await DatabaseManager.instance.WriteQuery(query, cancel) > 0;
        }

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

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public Waypoint() { }
    }
}
