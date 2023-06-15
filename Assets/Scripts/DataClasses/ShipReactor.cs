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
            List<object> fields = DatabaseManager.instance.SelectQuery("SELECT name, description, powerOutput, power, crew, slots FROM ShipReactor" +
                   $"LEFT JOIN ShipRequirements Requirement ON ShipReactor.requirements=Requirement.rowid WHERE ShipReactor.symbol='{smbl}' LIMIT 1;", System.Threading.CancellationToken.None).Result[0];
            symbol = Enum.Parse<ReactorType>(smbl);
            name = (string) fields[0];
            description = (string) fields[1];
            output = (int) fields[2];
            requirements = new ShipRequirements((int) fields[3], (int) fields[4], (int) fields[5]);
            condition = cond;
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipReactor() { }
    }
}
