using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipEngine
    {
        public string symbol;
        public string name;
        public string description;
        public int condition;
        public int speed;
        public ShipRequirements requirements;

        public ShipEngine( string smbl, int cond ) {
            List<object> fields = DatabaseManager.instance.SelectQuery("SELECT name, description, speed, power, crew, slots, Req.rowid FROM ShipEngine" +
                $"LEFT JOIN ShipRequirements Req ON ShipEngine.requirements=Req.rowid WHERE ShipEngine.symbol={smbl} LIMIT 1;", System.Threading.CancellationToken.None).Result[0];
            symbol = smbl;
            name = (string) fields[0];
            description = (string) fields[1];
            speed = Convert.ToInt32(fields[2]);
            requirements = new ShipRequirements(Convert.ToInt32(fields[3]), Convert.ToInt32(fields[4]), Convert.ToInt32(fields[5]), Convert.ToInt32(fields[6]));
            condition = cond;
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipEngine() { }
    }
}
