using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class SolarSystem : IDataClass
    {
        private readonly static Dictionary<string, SolarSystem> Instances = new Dictionary<string, SolarSystem>();

        public enum StarType { NEUTRON_STAR, RED_STAR, ORANGE_STAR, BLUE_STAR, YOUNG_STAR, WHITE_DWARF, BLACK_HOLE, HYPERGIANT, NEBULA, UNSTABLE };
        public string symbol;
        public string sectorSymbol;
        public StarType type;
        public int x;
        public int y;
        public List<Waypoint> waypoints;
        public List<string> factions;

        private SolarSystem( List<object> p, List<Waypoint> wps, List<string> fs ) {
            Update(p, wps, fs);
            Instances.Add(symbol, this);
        }

        private SolarSystem Update( List<object> p, List<Waypoint> wps, List<string> fs ) {
            symbol = (string) p[0];
            sectorSymbol = (string) p[1];
            type = Enum.Parse<StarType>((string) p[2]);
            x = Convert.ToInt32(p[3]);
            y = Convert.ToInt32(p[4]);
            waypoints = wps;
            factions = fs;
            return this;
        }

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge, CancellationToken cancel ) {
            if(maxAge != TimeSpan.MaxValue) {
                Debug.LogWarning("Requesting Solar Systems from the cache with an age, but their data doesn't expire.\nCall this function with TimeSpan.MaxValue as the second parameter to supress this message.");
            }
            string systemSymbol = "";
            if(endpoint.Trim('/') != "systems" && endpoint.Trim('/') != "systems.json") {
                // We're asking for a specific system.
                systemSymbol = $" WHERE System.symbol='{endpoint.Split('/')[^1]}' LIMIT 1";
            }

            while(DatabaseManager.instance == null) {
                await Task.Yield();
            }
            List<List<object>> systems = await DatabaseManager.instance.SelectQuery("SELECT symbol, sectorSymbol, type, x, y FROM System" + systemSymbol, cancel);
            if(cancel.IsCancellationRequested) { return default; }
            if(systems == null || systems.Count == 0) {
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<List<object>> sqlRes;
            List<Waypoint> waypoints;
            List<string> factions;
            foreach(List<object> p in systems) {
                sqlRes = await DatabaseManager.instance.SelectQuery($"SELECT symbol FROM Waypoint WHERE Waypoint.systemSymbol='{p[0]}';", cancel);
                if(cancel.IsCancellationRequested) { return default; }
                waypoints = new List<Waypoint>();
                if(sqlRes != null) {
                    foreach(List<object> wp in sqlRes) {
                        waypoints.Add(await Waypoint.GetWaypointFromSymbol((string) wp[0], cancel));
                    }
                }

                sqlRes = await DatabaseManager.instance.SelectQuery($"SELECT faction FROM System_Faction_Relationship WHERE system='{p[0]}';", cancel);
                if(cancel.IsCancellationRequested) { return default; }
                if(sqlRes != null && sqlRes.Count > 0) {
                    factions = (List<string>) sqlRes[0].Cast<string>();
                } else {
                    factions = new List<string>();
                }

                if(Instances.ContainsKey((string) p[0])) {
                    ret.Add(Instances[(string) p[0]].Update(p, waypoints, factions));
                } else {
                    ret.Add(new SolarSystem(p, waypoints, factions));
                }
            }
            return ret;
        }
        public async Task<bool> SaveToCache( CancellationToken cancel ) {
            string query = $"INSERT OR IGNORE INTO System (symbol, sectorSymbol, type, x, y) VALUES ('{symbol}','{sectorSymbol}','{type}',{x},{y});";
            if(factions != null && factions.Count > 0) {
                query += "\nINSERT OR IGNORE INTO System_Faction_Relationship (system, faction) VALUES ";
            foreach(string f in factions) {
                query += $"('{symbol}','{f}'),";
            }
            query = query[0..^1] + ";";
            }

            // Send it!
            return await DatabaseManager.instance.WriteQuery(query, cancel) > 0;
        }

        public override string ToString() {
            return $"System[{symbol}] @ {x}, {y}. Type: {type}. {(waypoints != null ? waypoints.Count : 0)} waypoints.";
        }

        public static async Task<SolarSystem> GetSystemFromSymbol( string symbol, CancellationToken cancel ) {
            // Exists!
            if(Instances.ContainsKey(symbol)) {
                return Instances[symbol];
            }

            // TODO This code freezes the editor, somehow? I'm gonna have to add a debugger to this.
            /*
            (ServerResult res, SolarSystem sys) = await ServerManager.RequestSingle<SolarSystem>($"systems/{symbol}", TimeSpan.MaxValue, RequestMethod.GET, cancel);
            if(cancel.IsCancellationRequested || res.result != ServerResult.ResultType.SUCCESS) { return null; }
            Instances.Add(sys.symbol, sys);
            return sys;
            */
            return null; // DEBUG Workaround.
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public SolarSystem() { }
    }
}
