using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipReactor
    {
        public enum ReactorType { REACTOR_SOLAR_I, REACTOR_FUSION_I, REACTOR_FISSION_I, REACTOR_CHEMICAL_I, REACTOR_ANTIMATTER_I }
        public ReactorType symbol;
        public string name;
        public string description;
        public int condition;
        public int output;
        public ShipRequirements requirements;

        public ShipReactor( string smbl, int cond ) {
            List<List<object>> result = DatabaseManager.instance.SelectQuery("SELECT name, description, powerOutput, power, crew, slots FROM ShipReactor " +
                   $"LEFT JOIN ShipRequirements Requirement ON ShipReactor.requirements=Requirement.rowid WHERE ShipReactor.symbol='{smbl}' LIMIT 1;", System.Threading.CancellationToken.None).Result;
            if(result == null || result.Count < 1) {
                UnityEngine.Debug.LogError("Failed to grab a ShipReactor from the database.");
                return;
            }
            List<object> fields = result[0];

            symbol = Enum.Parse<ReactorType>(smbl);
            name = (string) fields[0];
            description = (string) fields[1];
            output = Convert.ToInt32(fields[2]);
            requirements = new ShipRequirements(Convert.ToInt32(fields[3]), Convert.ToInt32(fields[4]), Convert.ToInt32(fields[5]));
            condition = cond;
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipReactor() { }
    }
}
