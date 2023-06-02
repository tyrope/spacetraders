using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace STCommander
{
    public class Ship : IDataClass
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

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge ) {
            string shipSymbol="";
            if(endpoint.Trim('/') != "my/ships") {
                // We're asking for a specific ship.
                shipSymbol = $"AND Ship.symbol = '{endpoint.Split('/')[^1]}' ";
            }
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> ships = await DatabaseManager.instance.SelectQuery("SELECT Ship.symbol,Registration.name,Registration.factionSymbol,Registration.role,Nav.systemSymbol,Nav.waypointSymbol,Nav.route_destination,"
                + "Nav.route_departure,Nav.route_departureTime,Nav.route_arrival,Nav.status,Nav.flightMode,Crew.\"current\",Crew.required,Crew.capacity,Crew.rotation,Crew.morale,Crew.wages,Frame.symbol,Frame.name,"
                + "Frame.description,Ship.shipFrame_condition,Frame.moduleSlots,Frame.mountingPoints,Frame.fuelCapacity,FrameReq.power,FrameReq.crew,FrameReq.slots,Reactor.symbol,Reactor.name,Reactor.description,"
                + "Ship.shipReactor_Condition,Reactor.powerOutput,ReactorReq.power,ReactorReq.crew,ReactorReq.slots,Engine.symbol,Engine.name,Engine.description,Ship.shipEngine_Condition,Engine.speed,EngineReq.power,"
                + "EngineReq.crew,EngineReq.slots,Cargo.capacity,Cargo.units,Ship.fuelCurrent,Ship.fuelCapacity,Ship.fuelAmount,Ship.fuelTimestamp,Ship.lastEdited FROM Ship"
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
            foreach(List<object> p in ships) {
                cargo = await DatabaseManager.instance.SelectQuery("SELECT ContractDeliverGood.rowid, ContractDeliverGood.tradeSymbol, ContractDeliverGood.destinationSymbol, "
                    + "ContractDeliverGood.unitsRequired, ContractDeliverGood.unitsFulfilled FROM Contract, ContractDeliverGood, ContractDeliverGood_ContractTerms_relationship WHERE"
                    + " ContractDeliverGood_ContractTerms_relationship.good = ContractDeliverGood.rowid AND ContractDeliverGood_ContractTerms_relationship.terms = Contract.terms AND Contract.id=" + p[0]);
                ret.Add(new Contract(p, cargo));
            }
            return ret;
        }

        public Task<bool> SaveToCache() {
            throw new NotImplementedException();
        }
    }
}
