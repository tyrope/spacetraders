using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STCommander
{
    public class Ship : IDataClass
    {
        public static readonly Dictionary<string, Ship> Instances = new Dictionary<string, Ship>();

        public async Task<List<IDataClass>> LoadFromCache( string endpoint, TimeSpan maxAge, CancellationToken cancel ) {
            string shipSymbol = "";
            if(endpoint.Trim('/') != "my/ships") {
                // We're asking for a specific ship.
                shipSymbol = $" AND Ship.symbol='{endpoint.Split('/')[^1]}' LIMIT 1";
            }
            double highestUnixTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds - maxAge.TotalSeconds;
            List<List<object>> ships = await DatabaseManager.instance.SelectQuery("SELECT symbol,nav,frame,frameCondition,reactor,reactorCondition,engine,engineCondition," +
                "name,factionSymbol,role,\"current\",required,capacity,rotation,morale,wages,cargoCapacity,cargoUnits,fuelCurrent,fuelCapacity,fuelAmount,fuelTimestamp " +
                "FROM Ship LEFT JOIN ShipRegistration Reg ON Ship.registration=Reg.rowid LEFT JOIN ShipCrew Crew ON crew=Crew.rowid WHERE Ship.lastEdited<" + highestUnixTimestamp + shipSymbol, cancel);
            if(cancel.IsCancellationRequested) { return default; }
            if(ships.Count == 0) {
                return null;
            }

            List<IDataClass> ret = new List<IDataClass>();
            foreach(List<object> ship in ships) {
                List<List<object>> cargo = await DatabaseManager.instance.SelectQuery(
                    $"SELECT symbol, name, description, units FROM ShipCargoItem LEFT JOIN Ship_ShipCargoItem_relationship Rel ON Rel.shipCargoItem=ShipCargoItem.rowid WHERE Rel.ship='{ship[0]}';", cancel);
                if(cancel.IsCancellationRequested) { return default; }

                List<List<object>> modules = await DatabaseManager.instance.SelectQuery(
                    "SELECT symbol, capacity, range, name, description, power, crew, slots FROM ShipModule LEFT JOIN ShipRequirements Requirement ON ShipModule.requirements=Requirement.rowid" +
                    $"LEFT JOIN Ship_ShipModules_relationship Relationship ON Relationship.shipModule=ShipModule.Symbol WHERE Relationship.ship='{ship[0]}'", cancel);
                if(cancel.IsCancellationRequested) { return default; }

                List<ShipMount> mounts = new List<ShipMount>();
                foreach(List<object> mount in await DatabaseManager.instance.SelectQuery(
                    "SELECT symbol,name,description,strength,power,crew,slots FROM ShipMount LEFT JOIN ShipRequirements Req ON ShipMount.requirements=Req.rowid " +
                    $"LEFT JOIN Ship_ShipMounts_relationship Relationship ON Relationship.shipMount=shipMount.symbol WHERE Relationship.ship='{ship[0]}';", cancel)) {
                    if(cancel.IsCancellationRequested) { return default; }
                    mounts.Add(new ShipMount(
                        mount,
                        (await DatabaseManager.instance.SelectQuery($"SELECT Deposit FROM ShipMount_Deposits_relationship WHERE ShipMount_Deposits_relationship.shipMount='{mount[0]}';", cancel))[0]
                        ));
                }
                if(cancel.IsCancellationRequested) { return default; }
                ret.Add(new Ship(ship, modules, mounts, cargo));
            }
            return ret;
        }
        public Ship( List<object> fields, List<List<object>> mdls, List<ShipMount> mnts, List<List<object>> manifest ) {
            symbol = (string) fields[0];
            nav = new ShipNavigation((int) fields[1]);
            frame = new ShipFrame((string) fields[2], (int) fields[3]);
            reactor = new ShipReactor((string) fields[4], (int) fields[5]);
            engine = new ShipEngine((string) fields[6], (int) fields[7]);

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
                    description = (string) item[2],
                    units = (int) item[3]
                });
            }
            cargo.inventory = inv.ToArray();

            Instances.Add(symbol, this);
        }

        public async Task<bool> SaveToCache( CancellationToken cancel ) {
            // Grab the existing rowids.
            string query = "SELECT registration, nav, crew FROM Ship WHERE Symbol='{symbol}' LIMIT 1";
            List<List<object>> ret = await DatabaseManager.instance.SelectQuery(query, cancel);
            List<int> rowids = new List<int>();
            if(ret.Count < 1) {
                // We gotta do inserts and grab new rowids.

                // Registration
                query = $"INSERT INTO ShipRegistration (name, factionSymbol, role) VALUES ('{registration.name}', '{registration.factionSymbol}', '{registration.role}');";
                await DatabaseManager.instance.WriteQuery(query, cancel);
                rowids.Add(await DatabaseManager.instance.GetLatestRowid(cancel));

                // Nav
                query = "INSERT INTO ShipNav (systemSymbol,waypointSymbol,status,flightMode,destination,departure,departureTime,arrival) VALUES" +
                    $"('{nav.systemSymbol}','{nav.waypointSymbol}','{nav.status}','{nav.flightMode}','{nav.route.DestSymbol}','{nav.route.DeptSymbol}',{nav.route.departureTime},{nav.route.arrival});";
                await DatabaseManager.instance.WriteQuery(query, cancel);
                rowids.Add(await DatabaseManager.instance.GetLatestRowid(cancel));

                // Crew
                query = $"INSERT INTO ShipCrew (\"current\", required, capacity, rotation, morale, wages) VALUES ({crew.current}, {crew.required},{crew.capacity},'{crew.rotation}',{crew.morale},{crew.wages});";
                await DatabaseManager.instance.WriteQuery(query, cancel);
                rowids.Add(await DatabaseManager.instance.GetLatestRowid(cancel));
            } else {
                // Update using the existing rowids.
                rowids = (List<int>) ret[0].Cast<int>();
                query = $"UPDATE ShipRegistration SET name='{registration.name}', factionSymbol='{registration.factionSymbol}', role='{registration.role}' WHERE rowid={rowids[0]};";
                query += $"UPDATE ShipNav SET systemSymbol='{nav.systemSymbol}',waypointSymbol='{nav.waypointSymbol}',status='{nav.status}',flightMode='{nav.flightMode}',destination='{nav.route.DestSymbol}'," +
                    $"departure='{nav.route.DeptSymbol}',departureTime={nav.route.departureTime},arrival={nav.route.arrival} WHERE rowid={rowids[1]};";
                query += $"UPDATE ShipCrew SET \"current\"={crew.current},required={crew.required},capacity={crew.capacity},rotation='{crew.rotation}',morale={crew.morale},wages={crew.wages} WHERE rowid={rowids[2]};";
                await DatabaseManager.instance.WriteQuery(query, cancel);
            }

            // Root object
            query += "INSERT INTO Ship (symbol,registration,nav,crew,frame,reactor,engine,cargoCapacity,cargoUnits,fuelCurrent,fuelAmount,fuelTimestamp," +
                $"frameCondition,reactorCondition,engineCondition,lastEdited) VALUES ('{symbol}',{rowids[0]},{rowids[1]},{rowids[2]},'{frame.symbol}','{reactor.symbol}" +
                $"','{engine.symbol}',{cargo.capacity},{cargo.units},{fuel.current},{fuel.consumed.amount},{fuel.consumed.timestamp},STRFTIME('%s')) ON CONFLICT(symbol) DO UPDATE SET " +
                "cargoUnits=excluded.cargoUnits,fuelCurrent=excluded.fuelCurrent,fuelAmount=excluded.fuelAmount,fuelTimestamp=excluded.fuelTimestamp,shipFrame_condition=excluded.shipFrame_condition," +
                "shipReactor_condition=excluded.shipReactor_condition,shipEngine_condition=excluded.shipEngine_condition,lastEdited=excluded.lastEdited;";
            if(cancel.IsCancellationRequested) { return false; }

            // Send it!
            return await DatabaseManager.instance.WriteQuery(query, cancel) > 0;
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

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public Ship() { }
    }
}
