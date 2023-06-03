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
                shipSymbol = $"AND Ship.symbol='{endpoint.Split('/')[^1]}' LIMIT 1";
            }
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> ships = await DatabaseManager.instance.SelectQuery("SELECT Ship.symbol,Ship.shipNav,Ship.shipFrame,Ship.shipReactor,Ship.shipEngine,Ship.shipFrame_condition,Ship.shipReactor_Condition," +
                "Ship.shipEngine_Condition,Registration.name,Registration.factionSymbol,Registration.role,Crew.\"current\",Crew.required,Crew.capacity,Crew.rotation,Crew.morale,Crew.wages,Cargo.capacity,Cargo.units," +
                "Ship.fuelCurrent,Ship.fuelCapacity,Ship.fuelAmount,Ship.fuelTimestamp FROM Ship LEFT JOIN ShipRegistration Registration ON Ship.shipRegistration=Registration.rowid " +
                "LEFT JOIN ShipCrew Crew ON Ship.shipCrew=Crew.rowid LEFT JOIN ShipCargo Cargo ON Ship.shipCargo=Cargo.rowid WHERE Ship.lastEdited<" + highestUnixTimestamp + shipSymbol);
            if(ships.Count == 0) {
                Debug.Log($"Ship::LoadFromCache() -- No results.");
                return null;
            }

            List<IDataClass> ret = new List<IDataClass>();
            foreach(List<object> ship in ships) {
                List<object> navigation = (await DatabaseManager.instance.SelectQuery(
                    "SELECT systemSymbol,waypointSymbol,route_destination,route_departure,route_departure,route_departureTime,route_arrival,status,flightMode "
                    + $"FROM ShipNav WHERE ShipNav.rowid={ship[1]} LIMIT 1;"))[0];

                List<object> frame = (await DatabaseManager.instance.SelectQuery(
                    "SELECT ShipFrame.symbol,ShipFrame.name,ShipFrame.description,ShipFrame.moduleSlots,ShipFrame.mountingPoints,ShipFrame.fuelCapacity,Requirement.power,Requirement.crew,Requirement.slots "
                    + $"FROM ShipFrame LEFT JOIN ShipRequirements Requirement ON ShipFrame.requirements=Requirement.rowid WHERE ShipFrame.rowid={ship[2]} LIMIT 1;"))[0];

                List<object> reactor = (await DatabaseManager.instance.SelectQuery(
                    "SELECT ShipReactor.symbol,ShipReactor.name,ShipReactor.description,ShipReactor.powerOutput,Requirement.power,Requirement.crew,Requirement.slots FROM ShipReactor "
                    + $"LEFT JOIN ShipRequirements Requirement ON ShipReactor.requirements=Requirement.rowid WHERE ShipReactor.rowid={ship[3]} LIMIT 1;"))[0];
                List<object> engine = (await DatabaseManager.instance.SelectQuery(
                    "SELECT ShipEngine.symbol,ShipEngine.name,ShipEngine.description,ShipEngine.speed,Requirement.power,Requirement.crew,Requirement.slots FROM ShipEngine "
                    + $"LEFT JOIN ShipRequirements Requirement ON ShipEngine.requirements=Requirement.rowid WHERE ShipEngine.rowid={ship[4]} LIMIT 1;"))[0];

                List<List<object>> cargo = await DatabaseManager.instance.SelectQuery(
                    "SELECT Item.symbol, Item.name, Item.description, Item.units FROM Ship INNER JOIN ShipCargo Cargo ON Cargo.rowid=Ship.shipCargo "
                    + $"INNER JOIN ShipCargo_ShipCargoItem_relationship Relation ON Relation.shipCargo=Cargo.rowid INNER JOIN ShipCargoItem Item ON Item.rowid=Relation.shipCargoItem WHERE Ship.symbol='{ship[0]}';");

                ret.Add(new Ship(ship, navigation, frame, reactor, engine, cargo));
            }
            return ret;
        }
        public Ship( List<object> fields, List<object> nvgtn, List<object> frm, List<object> rctr, List<object> eng, List<List<object>> manifest ) {
            symbol = (string) fields[0];
            registration.name = (string) fields[1];
            registration.factionSymbol = (string) fields[2];
            registration.role = (Role) fields[3];
            crew.current = (int) fields[5];
            crew.required = (int) fields[6];
            crew.capacity = (int) fields[7];
            crew.rotation = Enum.Parse<Crew.Rotation>((string) fields[8]);
            crew.morale = (int) fields[9];
            cargo.capacity = (int) fields[41];
            cargo.units = (int) fields[42];
            fuel.current = (int) fields[43];
            fuel.capacity = (int) fields[44];
            fuel.consumed.amount = (int) fields[45];
            fuel.consumed.timestamp = (string) fields[46];

            nav = new ShipNavigation(nvgtn);
            frame = new ShipFrame(frm, (int) fields[11]);
            reactor = new ShipReactor(rctr, (int) fields[13]);
            engine = new ShipEngine(eng, (int) fields[156]);

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
        public ShipNavigation nav;
        public Crew crew;
        public ShipFrame frame;
        public ShipReactor reactor;
        public ShipEngine engine;
        public Module[] Modules;
        public Mount[] Mounts;
        public Cargo cargo;
        public Fuel fuel;
    }
}
