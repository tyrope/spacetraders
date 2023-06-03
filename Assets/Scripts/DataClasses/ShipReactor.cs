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
        public Ship.Requirements requirements;

        public ShipReactor( List<object> fields, int cond ) {
            symbol = Enum.Parse<ReactorType>((string) fields[0]);
            name = (string) fields[1];
            description = (string) fields[2];
            condition = cond;
            output = (int) fields[3];
            requirements.power = (int) fields[4];
            requirements.crew = (int) fields[5];
            requirements.slots = (int) fields[6];
        }
    }
}
