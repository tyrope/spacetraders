using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class Faction : IDataClass
    {
        public static readonly Dictionary<string, Faction> Instances = new Dictionary<string, Faction>();
        public string symbol;
        public string name;
        public string description;
        public string headquarters;
        public List<Trait> traits;
        public bool isRecruiting;


        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge, CancellationToken cancel ) {
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;

            string factionSymbol = "";
            if(endpoint.Trim('/') != "factions") {
                // We're asking for a specific faction.
                factionSymbol = $" AND Faction.symbol='{endpoint.Split('/')[^1]}' LIMIT 1";
            }

            List <List<object>> factions = await DatabaseManager.instance.SelectQuery($"SELECT symbol, name, description, headquarters, isRecruiting FROM Faction WHERE lastEdited<{highestUnixTimestamp}" + factionSymbol, cancel);
            if(cancel.IsCancellationRequested) { return default; }
            if(factions.Count == 0) {
                Debug.Log($"Faction::LoadFromCache() -- No results.");
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<List<object>> traits;
            foreach(List<object> p in factions) {
                traits = await DatabaseManager.instance.SelectQuery("SELECT FactionTrait.symbol, FactionTrait.name, FactionTrait.description FROM FactionTrait WHERE "
                    + $"FactionTrait.symbol=FactionTrait_Faction_relationship.trait AND FactionTrait_Faction_relationship.faction='{p[0]}';", cancel);
                if(cancel.IsCancellationRequested) { return default; }
                ret.Add(new Faction(p, traits));
            }
            return ret;
        }

        public async Task<bool> SaveToCache( CancellationToken cancel ) {
            // Traits
            string query = $"INSERT OR IGNORE INTO FactionTrait (symbol, name, description) VALUES";
            foreach(Trait t in traits) {
                query += $"('{t.symbol}', '{t.name}', '{t.description}'),";
            }
            query = query[0..^1] + ";\n"; // Replace last comma with a semicolon.

            // Root object.
            query += "INSERT OR IGNORE INTO Faction (symbol, name, description, headquarters, isRecruiting, lastEdited) VALUES ('"
                + $"{symbol}','{name}','{description}','{headquarters}',{(isRecruiting ? 1 : 0)}"
                + ",unixepoch(now));";

            // Send it!
            return await DatabaseManager.instance.WriteQuery(query, cancel) > 0;
        }
        public Faction( List<object> p, List<List<object>> traitList ) {
            symbol = (string) p[0];
            name = (string) p[1];
            description = (string) p[2];
            headquarters = (string) p[3];
            isRecruiting = (int) p[4] == 1;
            traits = new List<Trait>();
            foreach(List<string> trait in traitList.Cast<List<string>>()) {
                traits.Add(Trait.GetTrait(trait[0], trait[1], trait[2]));
            }
            Instances.Add(symbol, this);
        }


        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public Faction() { }
    }
}
