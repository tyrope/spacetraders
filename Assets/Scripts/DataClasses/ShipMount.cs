using System;
using System.Collections.Generic;

namespace STCommander
{
    public class ShipMount
    {
        public enum MountType
        {
            MOUNT_GAS_SIPHON_I, MOUNT_GAS_SIPHON_II, MOUNT_GAS_SIPHON_III, MOUNT_SURVEYOR_I, MOUNT_SURVEYOR_II,
            MOUNT_SURVEYOR_III, MOUNT_SENSOR_ARRAY_I, MOUNT_SENSOR_ARRAY_II, MOUNT_SENSOR_ARRAY_III, MOUNT_MINING_LASER_I,
            MOUNT_MINING_LASER_II, MOUNT_MINING_LASER_III, MOUNT_LASER_CANNON_I, MOUNT_MISSILE_LAUNCHER_I, MOUNT_TURRET_I
        }
        public enum Deposit
        {
            QUARTZ_SAND, SILICON_CRYSTALS, PRECIOUS_STONES, ICE_WATER, AMMONIA_ICE,
            IRON_ORE, COPPER_ORE, SILVER_ORE, ALUMINUM_ORE, GOLD_ORE,
            PLATINUM_ORE, DIAMONDS, URANITE_ORE, MERITIUM_ORE
        }
        public MountType symbol;
        public string name;
        public string description;
        public int strength;
        public Deposit[] deposits;
        public Ship.Requirements requirements;

        public ShipMount( List<object> fields, List<object> deps ) {
            symbol = Enum.Parse<MountType>((string) fields[0]);
            name = (string) fields[1];
            description = (string) fields[2];
            strength = (int) fields[3];

            List<Deposit> listdeps = new List<Deposit>();
            foreach(string dep in deps) {
                listdeps.Add(Enum.Parse<Deposit>(dep));
            }
            deposits = listdeps.ToArray();

            requirements.power = (int) fields[5];
            requirements.crew = (int) fields[6];
            requirements.slots = (int) fields[7];
        }
    }
}
