namespace STCommander
{
    public class Ship
    {
        public class Requirements
        {
            public int power;
            public int crew;
            public int slots;
        }
        public enum Role { FABRICATOR, HARVESTER, HAULER, INTERCEPTOR, EXCAVATOR, TRANSPORT, REPAIR, SURVEYOR, COMMAND, CARRIER, PATROL, SATELLITE, EXPLORER, REFINERY };
        public class Registration
        {
            public string name;
            public string factionSymbol;
            public Role role;
        }
        public class Navigation
        {
            public enum Status { IN_TRANSIT, IN_ORBIT, DOCKED };
            public enum FlightMode { DRIFT, STEALTH, CRUISE, BURN };
            public class Route
            {
                public Waypoint destination;
                public Waypoint departure;
                public string departureTime;
                public string arrival;
            }
            public string systemSymbol;
            public string waypointSymbol;
            public Route route;
            public Status status;
            public FlightMode flightMode;
        }
        public class Crew
        {
            public enum Rotation { STRICT, RELAXED };
            public int current;
            public int required;
            public int capacity;
            public Rotation rotation;
            public int morale;
            public int wages;
        }
        public class Frame {
            public enum FrameType {
                FRAME_PROBE, FRAME_DRONE, FRAME_INTERCEPTOR, FRAME_RACER, FRAME_FIGHTER,
                FRAME_FRIGATE, FRAME_SHUTTLE, FRAME_EXPLORER, FRAME_MINER, FRAME_LIGHT_FREIGHTER,
                FRAME_HEAVY_FREIGHTER, FRAME_TRANSPORT, FRAME_DESTROYER, FRAME_CRUISER, FRAME_CARRIER };


            public FrameType symbol;
            public string name;
            public string description;
            public int condition;
            public int moduleSlots;
            public int mountingPoints;
            public int fuelCapacity;
            public Requirements requirements;
        }
        public class Reactor {
            public enum ReactorType { REACTOR_SOLAR_I, REACTOR_FUSION_I, REACTOR_FISSION_I, REACTOR_CHEMICAL_I, REACTOR_ANTIMATTER_I }
            public ReactorType symbol;
            public string name;
            public string description;
            public int condition;
            public int output;
            public Requirements requirements;

        }
        public class Engine {
            public enum EngineType { ENGINE_IMPULSE_DRIVE_I, ENGINE_ION_DRIVE_I, ENGINE_ION_DRIVE_II, ENGINE_HYPER_DRIVE_I }
            public EngineType symbol;
            public string name;
            public string description;
            public int condition;
            public int speed;
            public Requirements requirements;
        }
        public class Module {
            public enum ModuleType {
                MODULE_MINERAL_PROCESSOR_I, MODULE_CARGO_HOLD_I, MODULE_CREW_QUARTERS_I, MODULE_ENVOY_QUARTERS_I, MODULE_PASSENGER_CABIN_I,
                MODULE_MICRO_REFINERY_I, MODULE_ORE_REFINERY_I, MODULE_FUEL_REFINERY_I, MODULE_SCIENCE_LAB_I, MODULE_JUMP_DRIVE_I,
                MODULE_JUMP_DRIVE_II, MODULE_JUMP_DRIVE_III, MODULE_WARP_DRIVE_I, MODULE_WARP_DRIVE_II, MODULE_WARP_DRIVE_III,
                MODULE_SHIELD_GENERATOR_I, MODULE_SHIELD_GENERATOR_II }
            public int capacity;
            public int range;
            public string name;
            public string description;
            public Requirements requirements;
        }
        public class Mount
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
            public Requirements requirements;
        }
        public class Cargo
        {
            public class CargoItem
            {
                public string symbol;
                public string name;
                public string description;
                public int units;
            }
            public int capacity;
            public int units;
            public CargoItem[] inventory;
        }
        public class Fuel
        {
            public class Consumed
            {
                public int amount;
                public string timestamp;
            }
            public int current;
            public int capacity;
            public Consumed consumed;
        }

        public string symbol;
        public Registration registration;
        public Navigation nav;
        public Crew crew;
        public Frame frame;
        public Reactor reactor;
        public Engine engine;
        public Module[] Modules;
        public Mount[] Mounts;
        public Cargo cargo;
        public Fuel fuel;
    }
}