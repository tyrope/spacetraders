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

        public ShipReactor( int rowid, int cond ) {
            List<object> fields = DatabaseManager.instance.SelectQuery(
                    "SELECT ShipReactor.symbol,ShipReactor.name,ShipReactor.description,ShipReactor.powerOutput,Requirement.power,Requirement.crew,Requirement.slots FROM ShipReactor "
                    + $"LEFT JOIN ShipRequirements Requirement ON ShipReactor.requirements=Requirement.rowid WHERE ShipReactor.rowid={rowid} LIMIT 1;").Result[0];
            symbol = Enum.Parse<ReactorType>((string) fields[0]);
            name = (string) fields[1];
            description = (string) fields[2];
            output = (int) fields[3];
            requirements = new ShipRequirements((int) fields[4], (int) fields[5], (int) fields[6]);
            condition = cond;
        }
    }
}
