using System.Collections.Generic;

namespace STCommander
{
    public class Trait
    {
        public string symbol;
        public string name;
        public string description;

        public Trait(List<object> p ) {
            symbol = (string) p[0];
            name = (string) p[1];
            description = (string) p[2];
        }
    }
}
