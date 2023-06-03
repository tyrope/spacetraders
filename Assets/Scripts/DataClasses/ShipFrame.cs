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
        public Ship.Requirements requirements;

        public ShipFrame( int rowid, int cond ) {
            List<object> fields = DatabaseManager.instance.SelectQuery(
                "SELECT ShipFrame.symbol,ShipFrame.name,ShipFrame.description,ShipFrame.moduleSlots,ShipFrame.mountingPoints,ShipFrame.fuelCapacity,Requirement.power,Requirement.crew,Requirement.slots "
                + $"FROM ShipFrame LEFT JOIN ShipRequirements Requirement ON ShipFrame.requirements=Requirement.rowid WHERE ShipFrame.rowid={rowid} LIMIT 1;").Result[0];
            symbol = Enum.Parse<FrameType>((string) fields[0]);
            name = (string) fields[1];
            description = (string) fields[2];
            moduleSlots = (int) fields[3];
            mountingPoints = (int) fields[4];
            fuelCapacity = (int) fields[5];
            requirements.power = (int) fields[6];
            requirements.crew = (int) fields[7];
            requirements.slots = (int) fields[8];
            condition = cond;
        }
    }
}