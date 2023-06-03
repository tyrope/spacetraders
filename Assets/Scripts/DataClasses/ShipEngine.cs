using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipEngine
    {
        public enum EngineType { ENGINE_IMPULSE_DRIVE_I, ENGINE_ION_DRIVE_I, ENGINE_ION_DRIVE_II, ENGINE_HYPER_DRIVE_I }
        public EngineType symbol;
        public string name;
        public string description;
        public int condition;
        public int speed;
        public Ship.Requirements requirements;

        public ShipEngine( int rowid, int cond ) {
            List<object> fields = DatabaseManager.instance.SelectQuery(
                "SELECT ShipEngine.symbol,ShipEngine.name,ShipEngine.description,ShipEngine.speed,Requirement.power,Requirement.crew,Requirement.slots FROM ShipEngine "
                + $"LEFT JOIN ShipRequirements Requirement ON ShipEngine.requirements=Requirement.rowid WHERE ShipEngine.rowid={rowid} LIMIT 1;").Result[0];
            symbol = Enum.Parse<ShipEngine.EngineType>((string) fields[0]);
            name = (string) fields[1];
            description = (string) fields[2];
            speed = (int) fields[3];
            requirements.power = (int) fields[4];
            requirements.crew = (int) fields[5];
            requirements.slots = (int) fields[6];
            condition = cond;
        }
    }
}
