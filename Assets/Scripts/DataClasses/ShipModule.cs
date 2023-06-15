using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipModule
    {
        public string symbol;
        public int capacity;
        public int range;
        public string name;
        public string description;
        public ShipRequirements requirements;

        public ShipModule( List<object> fields) {
            symbol = (string) fields[0];
            capacity = Convert.ToInt32(fields[1]);
            range = Convert.ToInt32(fields[2]);
            name = (string) fields[3];
            description = (string) fields[4];

            requirements = new ShipRequirements(Convert.ToInt32(fields[5]), Convert.ToInt32(fields[6]), Convert.ToInt32(fields[7]));
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipModule() { }
    }
}