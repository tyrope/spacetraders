using System.Collections.Generic;

namespace STCommander
{
    public class Trait
    {
        private readonly static Dictionary<string, Trait> instances = new Dictionary<string, Trait>();
        public string symbol;
        public string name;
        public string description;

        private Trait(string smbl, string n, string desc) {
            symbol = smbl;
            name = n;
            description = desc;
        }

        public static Trait GetTrait(string smbl, string n, string desc ) {
            if(instances.ContainsKey(smbl) == false) {
                instances.Add(smbl, new Trait(smbl, n, desc));
            }
            return instances[smbl];
        }
    }
}
