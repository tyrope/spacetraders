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
        public ShipRequirements requirements;

        public ShipEngine( string smbl, int cond ) {
            List<object> fields = DatabaseManager.instance.SelectQuery("SELECT name, description, soeed, power, crew, slots FROM ShipEngine" +
                $"LEFT JOIN ShipRequirements Requirement ON ShipEngine.requirements=Requirement.rowid WHERE ShipEngine.symbol={smbl} LIMIT 1;", System.Threading.CancellationToken.None).Result[0];
            symbol = Enum.Parse<ShipEngine.EngineType>(smbl);
            name = (string) fields[0];
            description = (string) fields[1];
            speed = (int) fields[2];
            requirements = new ShipRequirements((int) fields[3], (int) fields[4], (int) fields[5]);
            condition = cond;
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipEngine() { }
    }
}
