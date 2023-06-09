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
        public readonly static Dictionary<string, SolarSystem> Instances = new Dictionary<string, SolarSystem>();

        public enum StarType { NEUTRON_STAR, RED_STAR, ORANGE_STAR, BLUE_STAR, YOUNG_STAR, WHITE_DWARF, BLACK_HOLE, HYPERGIANT, NEBULA, UNSTABLE };
        public string symbol;
        public string sectorSymbol;
        public StarType type;
        public int x;
        public int y;
        public List<string> waypoints;
        public List<string> factions;

        private SolarSystem( List<object> p, List<string> wps, List<string> fs ) {
            Update(p, wps, fs);
            Instances.Add(symbol, this);
        }

        private SolarSystem Update( List<object> p, List<string> wps, List<string> fs ) {
            symbol = (string) p[0];
            sectorSymbol = (string) p[1];
            type = Enum.Parse<StarType>((string) p[2]);
            x = (int) p[3];
            y = (int) p[4];
            waypoints = wps;
            factions = fs;
            return this;
        }

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge, CancellationToken cancel ) {
            if(maxAge != TimeSpan.MaxValue) {
                Debug.LogWarning("Requesting Solar Systems from the cache with an age, but their data doesn't expire.\nCall this function with TimeSpan.MaxValue as the second parameter to supress this message.");
            }
            string systemSymbol = "";
            if(endpoint.Trim('/') != "systems") {
                // We're asking for a specific faction.
                systemSymbol = $" WHERE System.symbol='{endpoint.Split('/')[^1]}' LIMIT 1";
            }

            List<List<object>> systems = await DatabaseManager.instance.SelectQuery("SELECT symbol, sectorSymbol, type, x, y FROM Systems" + systemSymbol, cancel);
            if(cancel.IsCancellationRequested) { return default; }
            if(systems.Count == 0) {
                Debug.Log($"SolarSystem::LoadFromCache() -- No results.");
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<string> waypoints;
            List<string> factions;
            foreach(List<object> p in systems) {
                waypoints = (List<string>) (await DatabaseManager.instance.SelectQuery($"SELECT symbol FROM Waypoint WHERE Waypoint.systemSymbol='{p[0]}';", cancel))[0].Cast<string>();
                if(cancel.IsCancellationRequested) { return default; }
                factions = (List<string>) (await DatabaseManager.instance.SelectQuery($"SELECT faction FROM System_Faction_Relationship WHERE system='{p[0]}';", cancel))[0].Cast<string>();
                if(cancel.IsCancellationRequested) { return default; }
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
            query += $"INSERT OR IGNORE INTO System_Faction_Relationship (system, faction) VALUES";
            foreach(string f in factions) {
                //TODO You were working on this.
                throw new NotImplementedException();
            }

            // Send it!
            return await DatabaseManager.instance.WriteQuery(query, cancel) > 0;
        }

        public override string ToString() {
            return $"System[{symbol}] @ {x}, {y}. Type: {type}. {waypoints.Count} waypoints.";
        }
    }
}
