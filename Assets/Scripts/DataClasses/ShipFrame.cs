using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipFrame
    {
        public enum FrameType
        {
            FRAME_PROBE, FRAME_DRONE, FRAME_INTERCEPTOR, FRAME_RACER, FRAME_FIGHTER,
            FRAME_FRIGATE, FRAME_SHUTTLE, FRAME_EXPLORER, FRAME_MINER, FRAME_LIGHT_FREIGHTER,
            FRAME_HEAVY_FREIGHTER, FRAME_TRANSPORT, FRAME_DESTROYER, FRAME_CRUISER, FRAME_CARRIER
        };
        public FrameType symbol;
        public string name;
        public string description;
        public int condition;
        public int moduleSlots;
        public int mountingPoints;
        public int fuelCapacity;
        public ShipRequirements requirements;

        public ShipFrame( string smbl, int cond ) {
            List<object> fields = DatabaseManager.instance.SelectQuery("SELECT name, description, moduleSlots, mountingPoints, fuelCapacity, power, crew, slots FROM ShipFrame" +
                $"LEFT JOIN ShipRequirements Requirement ON ShipFrame.requirements=Requirement.rowid WHERE symbol='{smbl}' LIMIT 1;", System.Threading.CancellationToken.None).Result[0];
            symbol = Enum.Parse<FrameType>(smbl);
            name = (string) fields[0];
            description = (string) fields[1];
            moduleSlots = Convert.ToInt32(fields[2]);
            mountingPoints = Convert.ToInt32(fields[3]);
            fuelCapacity = Convert.ToInt32(fields[4]);
            requirements = new ShipRequirements(Convert.ToInt32(fields[5]), Convert.ToInt32(fields[6]), Convert.ToInt32(fields[7]));
            condition = cond;
        }

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public ShipFrame() { }
    }
}