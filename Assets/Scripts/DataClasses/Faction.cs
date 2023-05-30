using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class Faction : IDataClass
    {
        public string symbol;
        public string name;
        public string description;
        public string headquarters;
        public List<Trait> traits;
        public bool isRecruiting;


        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge ) {
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> factions = await DatabaseManager.instance.SelectQuery($"SELECT symbol, name, description, headquarters, isRecruiting FROM Faction WHERE lastEdited<{highestUnixTimestamp}");
            if(factions.Count == 0) {
                Debug.Log($"Faction::LoadFromCache() -- No results.");
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<List<object>> traits;
            foreach(List<object> p in factions) {
                traits = await DatabaseManager.instance.SelectQuery("SELECT FactionTrait.symbol, FactionTrait.name, FactionTrait.description FROM Faction, FactionTrait, FactionTrait_Faction_relationship WHERE "
                    + $"FactionTrait.symbol=FactionTrait_Faction_relationship.trait AND FactionTrait_Faction_relationship.faction='{p[0]}';");
                ret.Add(new Faction(p, traits));
            }
            return ret;
        }

        public async Task<bool> SaveToCache() {
            string query = "BEGIN TRANSACTION;\n"; // This might be a big update. Do not let anybody else interfere.

            // Traits
            query += $"REPLACE INTO FactionTrait (symbol, name, description) VALUES";
            foreach(Trait t in traits) {
                    query += $"('{t.symbol}', '{t.name}', '{t.description}',";
            }
            query = query[0..^1] + ";\n"; // Replace last comma with a semicolon.

            // Root object.
            query += $"REPLACE INTO Faction (symbol, name, description, headquarters, isRecruiting, lastEdited) VALUES ('{symbol}','{name}','{description}','{headquarters}',{(isRecruiting ? 1 : 0)},unixepoch(now))";

            query += "COMMIT;\n"; // Send it!
            return await DatabaseManager.instance.WriteQuery(query) > 0;
        }
        public Faction( List<object> p, List<List<object>> traitList ) {
            symbol = (string) p[0];
            name = (string) p[1];
            description = (string) p[2];
            headquarters = (string) p[3];
            isRecruiting = (int) p[4] == 1;
            traits = new List<Trait>();
            foreach(List<object> trait in traitList) {
                traits.Add(new Trait(trait));
            }
        }
    }
}
