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
        public Ship.Requirements requirements;

        public ShipFrame( List<object> p, int cond ) {
            description = (string) p[1];
            moduleSlots = (int) p[2];
            mountingPoints = (int) p[3];
            fuelCapacity = (int) p[4];
            requirements.power = (int) p[5];
            requirements.crew = (int) p[6];
            requirements.slots = (int) p[7];
            condition = cond;
        }
    }
}