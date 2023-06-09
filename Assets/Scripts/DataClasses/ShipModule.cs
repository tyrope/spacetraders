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
            capacity = (int) fields[1];
            range = (int) fields[2];
            name = (string) fields[3];
            description = (string) fields[4];

            requirements = new ShipRequirements((int) fields[5], (int) fields[6], (int) fields[7]);
        }
    }
}