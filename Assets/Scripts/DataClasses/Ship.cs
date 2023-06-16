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
                shipSymbol = $" WHERE Ship.symbol='{endpoint.Split('/')[^1]}' LIMIT 1";
            }
            double highestUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - maxAge.TotalSeconds;
            List<List<object>> ships = await DatabaseManager.instance.SelectQuery("SELECT symbol,frame,frameCondition,reactor,reactorCondition,engine,engineCondition," +
                "name,factionSymbol,role,\"current\",required,capacity,rotation,morale,wages,cargoCapacity,cargoUnits,fuelCurrent,fuelCapacity,fuelAmount,fuelTimestamp,lastEdited " +
                "FROM Ship LEFT JOIN ShipRegistration Reg ON Reg.shipSymbol=symbol LEFT JOIN ShipCrew Crew ON Crew.shipSymbol=symbol" + shipSymbol, cancel);
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
                    "SELECT symbol, capacity, range, name, description, power, crew, slots, Req.rowid FROM ShipModule LEFT JOIN ShipRequirements Req ON ShipModule.requirements=Req.rowid " +
                    $"LEFT JOIN Ship_ShipModules_relationship Relationship ON Relationship.shipModule=ShipModule.Symbol WHERE Relationship.ship='{ship[0]}'", cancel);
                if(cancel.IsCancellationRequested) { return default; }

                List<ShipMount> mounts = new List<ShipMount>();
                foreach(List<object> mount in await DatabaseManager.instance.SelectQuery(
                    "SELECT symbol,name,description,strength,power,crew,slots,Req.rowid FROM ShipMount LEFT JOIN ShipRequirements Req ON ShipMount.requirements=Req.rowid " +
                    $"LEFT JOIN Ship_ShipMounts_relationship Relationship ON Relationship.shipMount=shipMount.symbol WHERE Relationship.ship='{ship[0]}';", cancel)) {
                    if(cancel.IsCancellationRequested) { return default; }
                    mounts.Add(new ShipMount(
                        mount,
                        (await DatabaseManager.instance.SelectQuery($"SELECT Deposit FROM ShipMount_Deposits_relationship WHERE shipMount='{mount[0]}';", cancel))[0]
                        ));
                }
                if(cancel.IsCancellationRequested) { return default; }
                ret.Add(new Ship(ship, modules, mounts, cargo));
            }
            return ret;
        }

        public Ship( List<object> fields, List<List<object>> mdls, List<ShipMount> mnts, List<List<object>> manifest ) {
            symbol = (string) fields[0];
            nav = new ShipNavigation(symbol);
            frame = new ShipFrame((string) fields[1], Convert.ToInt32(fields[2]));
            reactor = new ShipReactor((string) fields[3], Convert.ToInt32(fields[4]));
            engine = new ShipEngine((string) fields[5], Convert.ToInt32(fields[6]));

            registration.name = (string) fields[7];
            registration.factionSymbol = (string) fields[8];
            registration.role = Enum.Parse<Role>((string) fields[9]);
            crew.current = Convert.ToInt32(fields[10]);
            crew.required = Convert.ToInt32(fields[11]);
            crew.capacity = Convert.ToInt32(fields[12]);
            crew.rotation = Enum.Parse<Crew.Rotation>((string) fields[13]);
            crew.morale = Convert.ToInt32(fields[14]);
            crew.wages = Convert.ToInt32(fields[15]);
            cargo.capacity = Convert.ToInt32(fields[16]);
            cargo.units = Convert.ToInt32(fields[17]);
            fuel.current = Convert.ToInt32(fields[18]);
            fuel.capacity = Convert.ToInt32(fields[19]);
            fuel.consumed.amount = Convert.ToInt32(fields[20]);
            fuel.consumed.timestamp = (string) fields[21];
            lastEdited = Convert.ToInt32(fields[22]);

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
                    units = Convert.ToInt32(item[3])
                });
            }
            cargo.inventory = inv.ToArray();

            Instances.Add(symbol, this);
        }

        public async Task<bool> SaveToCache( CancellationToken cancel ) {
            string query = "BEGIN TRANSACTION;\n";

            await SaveCargoItems(cancel); // This needs to be done seperately because we need row ids, which we don't get in a transaction like this.
            if(cargo.inventory != null && cargo.inventory.Length > 0) {
                // Ship_ShipCargoItem_relationship: shipCargoItem (INTEGER NOT NULL), ship (TEXT NOT NULL)
                query += "INSERT OR IGNORE INTO Ship_ShipCargoItem_relationship (shipCargoItem, ship) VALUES ";
                foreach(Cargo.CargoItem item in cargo.inventory) {
                    query += $"({item.rowid}, '{symbol}'),";
                }
                query = query[0..^1] + ";"; // Replace last comma with semicolon.
            }

            // Ship: symbol (TEXT NOT NULL), frame (TEXT), reactor (TEXT), engine (TEXT NOT NULL), cargoCapacity (INTEGER NOT NULL), cargoUnits (INTEGER NOT NULL), fuelCurrent (INTEGER NOT NULL),
            //       fuelCapacity (INTEGER NOT NULL), fuelAmount (INTEGER), fuelTimestamp (INTEGER), frameCondition (INTEGER), reactorCondition (INTEGER), engineCondition (INTEGER NOT NULL), lastEdited (INTEGER NOT NULL)
            query += "INSERT INTO Ship (symbol,frame,reactor,engine,cargoCapacity,cargoUnits,fuelCurrent,fuelCapacity,fuelAmount,fuelTimestamp,frameCondition,reactorCondition,engineCondition,lastEdited) VALUES ('" +
                $"{symbol}','{frame.symbol}','{reactor.symbol}','{engine.symbol}',{cargo.capacity},{cargo.units},{fuel.current},{fuel.capacity},{fuel.consumed.amount},{fuel.consumed.TimestampNumerical},{frame.condition}," +
                $"{reactor.condition},{engine.condition},STRFTIME('%s')) ON CONFLICT(symbol) DO UPDATE SET frame=excluded.frame,reactor=excluded.reactor,engine=excluded.engine,cargoCapacity=excluded.cargoCapacity," +
                "cargoUnits=excluded.cargoUnits,fuelCurrent=excluded.fuelCurrent,fuelCapacity=excluded.fuelCapacity,fuelAmount=excluded.fuelAmount,fuelTimestamp=excluded.fuelTimestamp,frameCondition=excluded.frameCondition," +
                "reactorCondition=excluded.reactorCondition,engineCondition=excluded.engineCondition,lastEdited=excluded.lastEdited;\n";

            // ShipCrew: shipSymbol (TEXT NOT NULL), current (INTEGER NOT NULL), required (INTEGER NOT NULL), capacity (INTEGER NOT NULL), rotation (TEXT NOT NULL), morale (INTEGER NOT NULL), wages (INTEGER NOT NULL)
            query += $"INSERT INTO ShipCrew (shipSymbol,\"current\",required,capacity,rotation,morale,wages) VALUES ('{symbol}',{crew.current},{crew.required},{crew.capacity},'{crew.rotation}',{crew.morale},{crew.wages})" +
                "ON CONFLICT(shipSymbol) DO UPDATE SET \"current\"=excluded.current,required=excluded.required,capacity=excluded.capacity,rotation=excluded.rotation,morale=excluded.morale,wages=excluded.wages;\n";

            // ShipEngine: symbol (TEXT NOT NULL), name (TEXT NOT NULL), description (TEXT NOT NULL), speed (INTEGER NOT NULL), requirements (INTEGER NOT NULL)
            query += $"INSERT OR IGNORE INTO ShipEngine (symbol, name, description, speed, requirements) VALUES ('{engine.symbol}','{engine.name}','{engine.description}',{engine.speed},{engine.requirements.Rowid});";

            // ShipFrame: symbol (TEXT NOT NULL), name (TEXT NOT NULL), description (TEXT NOT NULL), moduleSlots (INTEGER NOT NULL), mountingPoints (INTEGER NOT NULL), fuelCapacity (INTEGER NOT NULL), requirements (INTEGER NOT NULL)
            query += "INSERT OR IGNORE INTO ShipFrame (symbol, name, description, moduleSlots, mountingPoints, fuelCapacity, requirements) VALUES " +
                $"('{frame.symbol}','{frame.name}','{frame.description}',{frame.moduleSlots},{frame.mountingPoints},{frame.fuelCapacity},{frame.requirements.Rowid});";

            // ShipModule: symbol (TEXT NOT NULL), capacity (INTEGER), range (INTEGER), name (TEXT NOT NULL), description (TEXT NOT NULL), requirements (INTEGER NOT NULL)
            if(Modules != null && Modules.Length > 0) {
                query += "INSERT OR IGNORE INTO ShipModule (symbol, capacity, range, name, description, requirements) VALUES ";
                foreach(ShipModule module in Modules) {
                    query += $"('{module.symbol}',{module.capacity},{module.range},'{module.name}','{module.description}',{module.requirements.Rowid}),";
                }
                query = query[0..^1] + ";"; // Replace last comma with semicolon.
            }

            if(Mounts != null && Mounts.Length > 0) {
                // ShipMount: symbol (TEXT NOT NULL), name (TEXT NOT NULL), description (TEXT NOT NULL), strength (INTEGER), requirements (INTEGER NOT NULL)
                query += "INSERT OR IGNORE INTO ShipMount (symbol,name,description,strength,requirements) VALUES ";
                foreach(ShipMount mount in Mounts) {
                    query += $"('{mount.symbol}','{mount.name}','{mount.description}',{mount.strength},{mount.requirements.Rowid}),";
                }

                // ShipMount_Deposits_relationship shipMount (TEXT NOT NULL), deposit (TEXT NOT NULL)
                // !!WHEN EDITING THIS, ALSO EDIT THE NUMBER AFTER THE FOREACH!!
                query += "INSERT OR IGNORE INTO ShipMount_Deposits_relationship (shipMount,deposit) VALUES ";
                bool revert = true;
                foreach(ShipMount mount in Mounts) {
                    if(mount.deposits == null) {
                        continue;
                    }
                    foreach(string deposit in mount.deposits) {
                        query += $"('{mount.name}','{deposit}'),";
                        revert = false;
                    }
                }
                if(revert) {
                    query = query[0..^81]; // Remove latest query (which is currently 81 characters.)
                } else {
                    query = query[0..^1] + ";"; // Replace last comma with semicolon.
                }
            }

            // ShipNav: shipSymbol (TEXT NOT NULL), systemSymbol (TEXT NOT NULL), waypointSymbol (TEXT NOT NULL), status (TEXT NOT NULL), flightMode (TEXT NOT NULL), destination (TEXT NOT NULL),
            //          departure (TEXT NOT NULL), departureTime (INTEGER NOT NULL), arrival (INTEGER NOT NULL)
            query += "INSERT INTO ShipNav (shipSymbol,systemSymbol,waypointSymbol,status,flightMode,destination,departure,departureTime,arrival) VALUES ('" +
                $"{symbol}','{nav.systemSymbol}','{nav.waypointSymbol}','{nav.status}','{nav.flightMode}','{nav.route.DestSymbol}','{nav.route.DeptSymbol}',{nav.route.departureTimestamp},{nav.route.arrivalTimestamp})" +
                "ON CONFLICT(shipSymbol) DO UPDATE SET systemSymbol=excluded.systemSymbol,waypointSymbol=excluded.waypointSymbol,status=excluded.status,flightMode=excluded.flightMode,destination=excluded.destination," +
                "departure=excluded.departure,departureTime=excluded.departureTime,arrival=excluded.arrival;\n";

            // ShipReactor: symbol (TEXT NOT NULL), name (TEXT NOT NULL), description (TEXT NOT NULL), powerOutput (INTEGER NOT NULL), requirements (INTEGER NOT NULL)

            // ShipRegistration:  shipSymbol (TEXT NOT NULL), name (TEXT NOT NULL), factionSymbol (TEXT NOT NULL), role (TEXT NOT NULL)
            query += $"INSERT INTO ShipRegistration (shipSymbol, name, factionSymbol, role) VALUES ('{symbol}', '{registration.name}', '{registration.factionSymbol}', '{registration.role}') " +
                "ON CONFLICT(shipSymbol) DO UPDATE SET name=excluded.name,factionSymbol=excluded.factionSymbol,role=excluded.role;\n";

            // Send it!
            query += "COMMIT;";
            return await DatabaseManager.instance.WriteQuery(query, cancel) > 0;
        }

        private async Task<bool> SaveCargoItems( CancellationToken cancel ) {
            // ShipCargoItem: symbol (TEXT NOT NULL), name (TEXT NOT NULL), description (TEXT NOT NULL), units (INTEGER NOT NULL)
            string query;
            int rows;
            foreach(Cargo.CargoItem item in cargo.inventory) {
                if(item.rowid < 0) { // invalid rowid.
                    query = "INSERT INTO ShipCargoItem (symbol, name, description, units) VALUES ('{item.symbol}','{item.name}','{item.description}',{item.units});";
                    rows = await DatabaseManager.instance.WriteQuery(query, cancel);
                    if(cancel.IsCancellationRequested || rows != 1) { return false; }
                    item.rowid = await DatabaseManager.instance.GetLatestRowid(cancel);
                    if(cancel.IsCancellationRequested) { return false; }
                } else {
                    query = $"UPDATE ShipCargoItem SET symbol='{item.symbol}',name='{item.name}',description='{item.description}', units={item.units} WHERE rowid={item.rowid} LIMIT 1";
                    rows = await DatabaseManager.instance.WriteQuery(query, cancel);
                    if(cancel.IsCancellationRequested || rows != 1) { return false; }
                }
            }
            return true;
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
                public int rowid = -1;
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
                public int TimestampNumerical => UnityEngine.Mathf.RoundToInt(DateTimeOffset.Parse(timestamp).ToUnixTimeSeconds());
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
        public int lastEdited;

        /// <summary>
        /// TO BE USED FOR REFLECTION PURPOSES ONLY!
        /// </summary>
        public Ship() {
            lastEdited = (int) DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }
}
