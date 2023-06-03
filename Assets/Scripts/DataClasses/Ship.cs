using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class Ship : IDataClass
    {
        public static readonly Dictionary<string, Ship> Instances = new Dictionary<string, Ship>();

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge ) {
            string shipSymbol = "";
            if(endpoint.Trim('/') != "my/ships") {
                // We're asking for a specific ship.
                shipSymbol = $"AND Ship.symbol = '{endpoint.Split('/')[^1]}' ";
            }
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> ships = await DatabaseManager.instance.SelectQuery("SELECT Ship.symbol,Registration.name,Registration.factionSymbol,Registration.role,Nav.systemSymbol,Nav.waypointSymbol,Nav.route_destination,"
                + "Nav.route_departure,Nav.route_departureTime,Nav.route_arrival,Nav.status,Nav.flightMode,Crew.\"current\",Crew.required,Crew.capacity,Crew.rotation,Crew.morale,Crew.wages,Frame.symbol,Frame.name,"
                + "Frame.description,Ship.shipFrame_condition,Frame.moduleSlots,Frame.mountingPoints,Frame.fuelCapacity,FrameReq.power,FrameReq.crew,FrameReq.slots,Reactor.symbol,Reactor.name,Reactor.description,"
                + "Ship.shipReactor_Condition,Reactor.powerOutput,ReactorReq.power,ReactorReq.crew,ReactorReq.slots,Engine.symbol,Engine.name,Engine.description,Ship.shipEngine_Condition,Engine.speed,EngineReq.power,"
                + "EngineReq.crew,EngineReq.slots,Cargo.capacity,Cargo.units,Ship.fuelCurrent,Ship.fuelCapacity,Ship.fuelAmount,Ship.fuelTimestamp FROM Ship "
                + "LEFT JOIN ShipRegistration Registration ON Ship.shipRegistration=Registration.rowid LEFT JOIN ShipNav Nav ON Ship.shipNav=Nav.rowid LEFT JOIN ShipCrew Crew ON Ship.shipCrew=Crew.rowid "
                + "LEFT JOIN ShipFrame Frame ON Ship.shipFrame=Frame.rowid LEFT JOIN ShipRequirements FrameReq ON Frame.requirements=FrameReq.rowid LEFT JOIN ShipReactor Reactor ON Ship.shipReactor=Reactor.rowid "
                + "LEFT JOIN ShipRequirements ReactorReq ON Reactor.requirements=ReactorReq.rowid LEFT JOIN ShipEngine Engine ON Ship.shipEngine=Engine.rowid "
                + "LEFT JOIN ShipRequirements EngineReq ON Engine.requirements=EngineReq.rowid LEFT JOIN ShipCargo Cargo ON Ship.shipCargo=Cargo.rowid WHERE Ship.lastEdited<" + highestUnixTimestamp + shipSymbol);
            if(ships.Count == 0) {
                Debug.Log($"Ship::LoadFromCache() -- No results.");
                return null;
            }
            List<IDataClass> ret = new List<IDataClass>();
            List<List<object>> cargo;
            foreach(List<object> ship in ships) {
                cargo = await DatabaseManager.instance.SelectQuery("SELECT Item.symbol, Item.name, Item.description, Item.units FROM Ship INNER JOIN ShipCargo Cargo ON Cargo.rowid=Ship.shipCargo "
                    + $"INNER JOIN ShipCargo_ShipCargoItem_relationship Relation ON Relation.shipCargo=Cargo.rowid INNER JOIN ShipCargoItem Item ON Item.rowid=Relation.shipCargoItem WHERE Ship.symbol='{ship[0]}';");
                ;
                ret.Add(new Ship(ship, cargo));
            }
            return ret;
        }
        public Ship( List<object> fields, List<List<object>> manifest) {
            symbol = (string) fields[0];
            registration.name = (string) fields[1];
            registration.factionSymbol = (string) fields[2];
            registration.role = (Role) fields[3];
            nav.systemSymbol = (string) fields[4];
            nav.waypointSymbol = (string) fields[5];
            nav.route.destination = (Waypoint) fields[6]; //TODO Get waypoint instance from symbol.
            nav.route.departure = (Waypoint) fields[7]; //TODO Get waypoint instance from symbol.
            nav.route.departureTime = (string) fields[8];
            nav.route.arrival = (string) fields[9];
            nav.status = Enum.Parse<Navigation.Status>((string) fields[10]);
            nav.flightMode = Enum.Parse<Navigation.FlightMode>((string) fields[11]);
            crew.current = (int) fields[12];
            crew.required = (int) fields[13];
            crew.capacity = (int) fields[14];
            crew.rotation = Enum.Parse<Crew.Rotation>((string) fields[15]);
            crew.morale = (int) fields[16];
            frame.description = (string) fields[17];
            frame.condition = (int) fields[18];
            frame.moduleSlots = (int) fields[19];
            frame.mountingPoints = (int) fields[20];
            frame.fuelCapacity = (int) fields[21];
            frame.requirements.power = (int) fields[22];
            frame.requirements.crew = (int) fields[23];
            frame.requirements.slots = (int) fields[24];
            reactor.symbol = Enum.Parse<Reactor.ReactorType>((string) fields[25]);
            reactor.name = (string) fields[26];
            reactor.description = (string) fields[27];
            reactor.condition = (int) fields[28];
            reactor.output = (int) fields[29];
            reactor.requirements.power = (int) fields[30];
            reactor.requirements.crew = (int) fields[31];
            reactor.requirements.slots = (int) fields[32];
            engine.symbol = Enum.Parse<Engine.EngineType>((string) fields[33]);
            engine.name = (string) fields[34];
            engine.description = (string) fields[35];
            engine.condition = (int) fields[36];
            engine.speed = (int) fields[37];
            engine.requirements.power = (int) fields[38];
            engine.requirements.crew = (int) fields[39];
            engine.requirements.slots = (int) fields[40];
            cargo.capacity = (int) fields[41];
            cargo.units = (int) fields[42];
            fuel.current = (int) fields[43];
            fuel.capacity = (int) fields[44];
            fuel.consumed.amount = (int) fields[45];
            fuel.consumed.timestamp = (string) fields[46];

            List<Cargo.CargoItem> inv = new List<Cargo.CargoItem>();
            foreach(List<object> item in manifest) {
                inv.Add(new Cargo.CargoItem() {
                    symbol = (string) item[0],
                    name = (string) item[1],
                    description = (string) item[2]
                });
            }
            cargo.inventory = inv.ToArray();

            Instances.Add(symbol, this);
        }

        public Task<bool> SaveToCache() {
            throw new NotImplementedException();
        }
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
                internal DateTime ETA => DateTime.Parse(arrival);
                internal TimeSpan TotalFlightTime => ETA - DateTime.Parse(departureTime);
            }
            public string systemSymbol;
            public string waypointSymbol;
            public Route route;
            public Status status;
            public FlightMode flightMode;
            internal TimeSpan CurrentFlightTime {
                get {
                    if(route.ETA - DateTime.UtcNow < TimeSpan.Zero) { return TimeSpan.Zero; } // Pre-flight.
                    if(route.ETA - DateTime.UtcNow > route.TotalFlightTime) { // Post-flight.
                        return route.TotalFlightTime;
                    }
                    return route.ETA - DateTime.UtcNow;
                }
            }
            public float FractionFlightComplete {
                get {
                    if(route.TotalFlightTime.Ticks == 0) { // Null distance.
                        return 1f;
                    }
                    return (float) (CurrentFlightTime / route.TotalFlightTime);
                }
            }

            public override string ToString() {
                switch(status) {
                    case Status.DOCKED:
                        return $"DOCKED @ {waypointSymbol}";
                    case Status.IN_ORBIT:
                        return $"ORBITING {waypointSymbol}";
                    case Status.IN_TRANSIT:
                        return $"{route.departure}→{route.destination} ({route.ETA:HH:mm:ss})";
                    default:
                        return "ERR_INVALID_NAV_STATUS";
                }
            }
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
        public class Frame
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
            public Requirements requirements;
        }
        public class Reactor
        {
            public enum ReactorType { REACTOR_SOLAR_I, REACTOR_FUSION_I, REACTOR_FISSION_I, REACTOR_CHEMICAL_I, REACTOR_ANTIMATTER_I }
            public ReactorType symbol;
            public string name;
            public string description;
            public int condition;
            public int output;
            public Requirements requirements;

        }
        public class Engine
        {
            public enum EngineType { ENGINE_IMPULSE_DRIVE_I, ENGINE_ION_DRIVE_I, ENGINE_ION_DRIVE_II, ENGINE_HYPER_DRIVE_I }
            public EngineType symbol;
            public string name;
            public string description;
            public int condition;
            public int speed;
            public Requirements requirements;
        }
        public class Module
        {
            public enum ModuleType
            {
                MODULE_MINERAL_PROCESSOR_I, MODULE_CARGO_HOLD_I, MODULE_CREW_QUARTERS_I, MODULE_ENVOY_QUARTERS_I, MODULE_PASSENGER_CABIN_I,
                MODULE_MICRO_REFINERY_I, MODULE_ORE_REFINERY_I, MODULE_FUEL_REFINERY_I, MODULE_SCIENCE_LAB_I, MODULE_JUMP_DRIVE_I,
                MODULE_JUMP_DRIVE_II, MODULE_JUMP_DRIVE_III, MODULE_WARP_DRIVE_I, MODULE_WARP_DRIVE_II, MODULE_WARP_DRIVE_III,
                MODULE_SHIELD_GENERATOR_I, MODULE_SHIELD_GENERATOR_II
            }
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

                public override string ToString() {
                    return $"{units}x {name}";
                }
            }
            public int capacity;
            public int units;
            public CargoItem[] inventory;

            public override string ToString() {
                return $"{units / (float) capacity * 100f:n2}%\n{units}/{capacity}";
            }
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

            public override string ToString() {
                return $"{current / (float) capacity * 100f:n2}%\n{current}/{capacity}";
            }
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
