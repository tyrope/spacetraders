using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipMount
    {
        public string symbol;
        public string name;
        public string description;
        public int strength;
        public string[] deposits;
        public ShipRequirements requirements;

        public ShipMount( List<object> fields, List<object> deps ) {
            symbol = (string) fields[0];
            name = (string) fields[1];
            description = (string) fields[2];
            strength = Convert.ToInt32(fields[3]);

            List<string> listdeps = new List<string>();
            foreach(string dep in deps) {
                listdeps.Add(dep);
            }
            deposits = listdeps.ToArray();

            requirements = new ShipRequirements(Convert.ToInt32(fields[5]), Convert.ToInt32(fields[6]), Convert.ToInt32(fields[7]), Convert.ToInt32(fields[8]));
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipMount() { }
    }
}
