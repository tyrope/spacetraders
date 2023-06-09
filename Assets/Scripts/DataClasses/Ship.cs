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
                shipSymbol = $" AND Ship.symbol='{endpoint.Split('/')[^1]}' LIMIT 1";
            }
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> ships = await DatabaseManager.instance.SelectQuery("SELECT Ship.symbol,Ship.shipNav,Ship.shipFrame,Ship.shipFrame_condition,Ship.shipReactor,Ship.shipReactor_Condition,Ship.shipEngine," +
                "Ship.shipEngine_Condition,Registration.name,Registration.factionSymbol,Registration.role,Crew.\"current\",Crew.required,Crew.capacity,Crew.rotation,Crew.morale,Crew.wages,Cargo.capacity,Cargo.units," +
                "Ship.fuelCurrent,Ship.fuelCapacity,Ship.fuelAmount,Ship.fuelTimestamp FROM Ship LEFT JOIN ShipRegistration Registration ON Ship.shipRegistration=Registration.rowid " +
                "LEFT JOIN ShipCrew Crew ON Ship.shipCrew=Crew.rowid LEFT JOIN ShipCargo Cargo ON Ship.shipCargo=Cargo.rowid WHERE Ship.lastEdited<" + highestUnixTimestamp + shipSymbol);
            if(ships.Count == 0) {
                Debug.Log($"Ship::LoadFromCache() -- No results.");
                return null;
            }

            List<IDataClass> ret = new List<IDataClass>();
            foreach(List<object> ship in ships) {
                List<List<object>> cargo = await DatabaseManager.instance.SelectQuery(
                    "SELECT Item.symbol, Item.name, Item.description, Item.units FROM Ship INNER JOIN ShipCargo Cargo ON Cargo.rowid=Ship.shipCargo "
                    + $"INNER JOIN ShipCargo_ShipCargoItem_relationship Relation ON Relation.shipCargo=Cargo.rowid INNER JOIN ShipCargoItem Item ON Item.rowid=Relation.shipCargoItem WHERE Ship.symbol='{ship[0]}';");

                List<List<object>> modules = await DatabaseManager.instance.SelectQuery(
                    "SELECT symbol, capacity, range, name, description, power, crew, slots FROM ShipModule" +
                    "INNER JOIN ShipRequirements Requirement ON ShipModule.requirements=Requirement.rowid" +
                    $"INNER JOIN Ship_ShipModules_relationship Relationship ON Relationship.shipModule=ShipModule.Symbol WHERE Relationship.ship='{ship[0]}'");


                List<ShipMount> mounts = new List<ShipMount>();
                foreach(List<object> mount in await DatabaseManager.instance.SelectQuery(
                    "SELECT ShipMount.symbol,ShipMount.name,ShipMount.description,ShipMount.strength,Requirement.power,Requirement.crew,Requirement.slots"
                    + "FROM Ship,ShipMount INNER JOIN ShipRequirements Requirement ON ShipMount.requirements=Requirement.rowid INNER JOIN Ship_ShipMounts_relationship Relationship ON Relationship.shipMount=shipMount.symbol"
                    + $"WHERE Relationship.ship='{ship[0]}';")) {
                    mounts.Add(new ShipMount(
                        mount,
                        (await DatabaseManager.instance.SelectQuery("SELECT Deposit FROM ShipMount_Deposits_relationship WHERE ShipMount_Deposits_relationship.shipMount='{mount[0]}';"))[0]
                        ));
                }
                ret.Add(new Ship(ship, modules, mounts, cargo));
            }
            return ret;
        }
        public Ship( List<object> fields, List<List<object>> mdls, List<ShipMount> mnts, List<List<object>> manifest ) {
            symbol = (string) fields[0];
            nav = new ShipNavigation((int) fields[1]);
            frame = new ShipFrame((int) fields[2], (int) fields[3]);
            reactor = new ShipReactor((int) fields[4], (int) fields[5]);
            engine = new ShipEngine((int) fields[6], (int) fields[7]);

            registration.name = (string) fields[8];
            registration.factionSymbol = (string) fields[9];
            registration.role = Enum.Parse<Role>((string) fields[10]);
            crew.current = (int) fields[11];
            crew.required = (int) fields[12];
            crew.capacity = (int) fields[13];
            crew.rotation = Enum.Parse<Crew.Rotation>((string) fields[14]);
            crew.morale = (int) fields[15];
            crew.wages = (int) fields[16];
            cargo.capacity = (int) fields[17];
            cargo.units = (int) fields[18];
            fuel.current = (int) fields[19];
            fuel.capacity = (int) fields[20];
            fuel.consumed.amount = (int) fields[21];
            fuel.consumed.timestamp = (string) fields[22];

            List<ShipModule> mods = new List<ShipModule>();
            foreach(List<object> module in mdls) {
                mods.Add(new ShipModule(module) {
                });
            }
            Modules = mods.ToArray();
            Mounts = mnts.ToArray();

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
        public ShipModule[] Modules;
        public ShipMount[] Mounts;
        public Cargo cargo;
        public Fuel fuel;
    }
}
