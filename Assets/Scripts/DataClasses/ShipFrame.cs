using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipFrame
    {
        public string symbol;
        public string name;
        public string description;
        public int condition;
        public int moduleSlots;
        public int mountingPoints;
        public int fuelCapacity;
        public ShipRequirements requirements;

        public ShipFrame( string smbl, int cond ) {
            List<List<object>> ret = DatabaseManager.instance.SelectQuery("SELECT name, description, moduleSlots, mountingPoints, fuelCapacity, power, crew, slots, Req.rowid FROM ShipFrame " +
                $"LEFT JOIN ShipRequirements Req ON ShipFrame.requirements=Req.rowid WHERE symbol='{smbl}' LIMIT 1;", System.Threading.CancellationToken.None).Result;
            if(ret != null && ret.Count < 1) {
                UnityEngine.Debug.LogError("SQL: Frame not found");
                return;
            }
            List<object> fields = ret[0];
            symbol = smbl;
            name = (string) fields[0];
            description = (string) fields[1];
            moduleSlots = Convert.ToInt32(fields[2]);
            mountingPoints = Convert.ToInt32(fields[3]);
            fuelCapacity = Convert.ToInt32(fields[4]);
            requirements = new ShipRequirements(Convert.ToInt32(fields[5]), Convert.ToInt32(fields[6]), Convert.ToInt32(fields[7]), Convert.ToInt32(fields[8]));
            condition = cond;
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipFrame() { }
    }
}