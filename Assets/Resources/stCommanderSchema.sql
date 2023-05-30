-- Create tables.
CREATE TABLE IF NOT EXISTS Agent (
    accountId TEXT NOT NULL,
    symbol TEXT NOT NULL PRIMARY KEY,
    headquarters TEXT NOT NULL REFERENCES Waypoint(symbol),
    credits INT NOT NULL,
    startingFaction TEXT NOT NULL REFERENCES Faction(symbol),
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS Chart (
    waypointSymbol TEXT NOT NULL PRIMARY KEY REFERENCES Waypoint(symbol),
    submittedBy TEXT,
    submittedOn INTEGER,
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ConnectedSystem (
    symbol TEXT NOT NULL PRIMARY KEY REFERENCES System(symbol),
    distance INT NOT NULL
);
CREATE TABLE IF NOT EXISTS Contract (
    id TEXT NOT NULL PRIMARY KEY,
    factionSymbol TEXT NOT NULL REFERENCES Faction(Symbol),
    type TEXT NOT NULL REFERENCES ContractType(value),
    terms INT NOT NULL REFERENCES ContractTerms(rowid),
    accepted INT NOT NULL CHECK (accepted IN (0,1)),
    fulfilled INT NOT NULL CHECK (fulfilled IN (0,1)),
    deadlineToAccept INT NOT NULL,
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ContractType (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS ContractDeliverGood (
    tradeSymbol TEXT NOT NULL REFERENCES TradeGood(symbol),
    destinationSymbol TEXT NOT NULL REFERENCES Waypoint(symbol),
    unitsRequired INT NOT NULL,
    unitsFulfilled INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ContractPayment (
    onAccepted INT NOT NULL,
    onFulfilled INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ContractTerms (
    deadline INT NOT NULL,
    payment INT NOT NULL REFERENCES ContractPayment(rowid)
);
CREATE TABLE IF NOT EXISTS ContractDeliverGood_ContractTerms_relationship (
    good INT NOT NULL UNIQUE REFERENCES ContractDeliverGood(rowid),
    terms INT NOT NULL REFERENCES ContractTerms(rowid)
);
CREATE TABLE IF NOT EXISTS Cooldown (
    shipSymbol TEXT NOT NULL PRIMARY KEY REFERENCES Ship(symbol),
    totalSeconds INT NOT NULL,
    remainingSeconds INT NOT NULL,
    expiration INT,
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS Extraction (
    shipSymbol TEXT NOT NULL REFERENCES Ship(symbol),
    yieldSymbol TEXT NOT NULL REFERENCES TradeSymbol(value),
    yieldUnits INT NOT NULL,
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS Faction (
    symbol TEXT NOT NULL PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    headquarters TEXT NOT NULL REFERENCES System(symbol),
    isRecruiting INT NOT NULL CHECK (isRecruiting=0 OR isRecruiting=1),
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS FactionTrait (
    symbol TEXT NOT NULL PRIMARY KEY REFERENCES FactionTraitSymbols(value),
    name TEXT NOT NULL,
    description TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS FactionTraitSymbols(
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS FactionTrait_Faction_relationship (
    faction TEXT NOT NULL REFERENCES Faction(symbol),
    trait TEXT NOT NULL REFERENCES FactionTrait(symbol)
);
CREATE TABLE IF NOT EXISTS JumpGate (
    jumpRange INT NOT NULL,
    factionSymbol TEXT REFERENCES Faction(symbol),
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ConnectedSystem_JumpGate_relationship (
    connectedSystem INT NOT NULL REFERENCES ConnectedSystem(rowid),
    jumpGate INT NOT NULL REFERENCES JumpGate(rowid)
);
CREATE TABLE IF NOT EXISTS Market (
    symbol TEXT NOT NULL PRIMARY KEY REFERENCES Waypoint(symbol),
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS Market_exports_relationship (
    market TEXT NOT NULL REFERENCES Market(symbol),
    tradeGood TEXT NOT NULL REFERENCES TradeGood(symbol)
);
CREATE TABLE IF NOT EXISTS Market_imports_relationship (
    market TEXT NOT NULL REFERENCES Market(symbol),
    tradeGood TEXT NOT NULL REFERENCES TradeGood(symbol)
);
CREATE TABLE IF NOT EXISTS Market_exchange_relationship (
    market TEXT NOT NULL REFERENCES Market(symbol),
    tradeGood TEXT NOT NULL REFERENCES TradeGood(symbol)
);
CREATE TABLE IF NOT EXISTS Market_transactions_relationship (
    market TEXT NOT NULL REFERENCES Market(symbol),
    marketTransaction INT NOT NULL UNIQUE REFERENCES MarketTransaction(rowid)
);
CREATE TABLE IF NOT EXISTS Market_tradegoods_relationship (
    market TEXT NOT NULL REFERENCES Market(symbol),
    tradeGood TEXT NOT NULL REFERENCES TradeGood(symbol)
);
CREATE TABLE IF NOT EXISTS MarketTradeGood (
    symbol TEXT NOT NULL REFERENCES TradeGood(symbol),
    tradeVolume INT NOT NULL,
    supply TEXT NOT NULL REFERENCES MarketTradeGoodSupply(value),
    purchasePrice INT NOT NULL,
    sellPrice INT NOT NULL
);
CREATE TABLE IF NOT EXISTS MarketTradeGoodSupply (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS MarketTransaction (
    waypointSymbol TEXT NOT NULL REFERENCES Market(symbol),
    shipSymbol TEXT NOT NULL REFERENCES Ship(symbol),
    tradeSymbol TEXT NOT NULL REFERENCES TradeSymbol(value),
    type TEXT NOT NULL REFERENCES MarketTransactionType(value),
    units INT NOT NULL,
    pricePerUnit INT NOT NULL,
    totalPrice INT NOT NULL,
    timestamp INT NOT NULL
);
CREATE TABLE IF NOT EXISTS MarketTransactionType (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS Ship (
    symbol TEXT NOT NULL PRIMARY KEY,
    shipRegistration INT NOT NULL UNIQUE REFERENCES ShipRegistration(rowid),
    shipNav INT NOT NULL UNIQUE REFERENCES ShipNav(rowid),
    shipCrew INT UNIQUE REFERENCES ShipCrew(rowid),
    shipFrame INT REFERENCES ShipFrame(rowid),
    shipReactor INT REFERENCES ShipReactor(rowid),
    shipEngine INT NOT NULL REFERENCES ShipEngine(rowid),
    shipCargo INT NOT NULL UNIQUE REFERENCES ShipCargo(rowid),
    fuelCurrent INT NOT NULL,
    fuelCapacity INT NOT NULL,
    fuelAmount INT,
    fuelTimestamp INT,

    shipFrame_condition INT,
    shipReactor_Condition INT,
    shipEngine_Condition INT NOT NULL,
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS Ship_ShipModules_relationship (
    ship INT NO NULL REFERENCES Ship(rowid),
    shipModule INT NO NULL REFERENCES ShipModule(rowid)
);
CREATE TABLE IF NOT EXISTS Ship_ShipMounts_relationship (
    ship INT NOT NULL REFERENCES Ship(rowid),
    shipMount INT NOT NULL REFERENCES ShipMount(rowid)
);
CREATE TABLE IF NOT EXISTS ShipCargo (
    capacity INT NOT NULL,
    units INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ShipCargoItem (
    symbol TEXT NOT NULL,
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    units INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ShipCargo_ShipCargoItem_relationship (
    shipCargoItem INT NOT NULL PRIMARY KEY REFERENCES ShipCargoItem(rowid),
    shipCargo INT NOT NULL REFERENCES ShipCargo(rowid)
);
CREATE TABLE IF NOT EXISTS ShipCrew (
    "current" INT NOT NULL,
    required INT NOT NULL,
    capacity INT NOT NULL,
    rotation TEXT NOT NULL REFERENCES ShipCrewRotation(value),
    morale INT NOT NULL,
    wages INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ShipCrewRotation (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS ShipEngine (
    symbol TEXT NOT NULL PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    speed INT NOT NULL,
    requirements INT NOT NULL UNIQUE REFERENCES ShipRequirements(rowid)
);
CREATE TABLE IF NOT EXISTS ShipFrame (
    symbol TEXT NOT NULL PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    moduleSlots INT NOT NULL,
    mountingPoints INT NOT NULL,
    fuelCapacity INT NOT NULL,
    requirements INT NOT NULL UNIQUE REFERENCES ShipRequirements(rowid)
);
CREATE TABLE IF NOT EXISTS ShipModule (
    symbol TEXT NOT NULL PRIMARY KEY,
    capacity INT,
    range INT,
    name TEXT NOT NULL,
    description TEXT,
    requirements INT NOT NULL UNIQUE REFERENCES ShipRequirements(rowid)
);
CREATE TABLE IF NOT EXISTS ShipMount (
    symbol TEXT NOT NULL PRIMARY KEY,
    name TEXT NOT NULL,
    strength INT,
    requirements INT NOT NULL REFERENCES ShipRequirements(rowid)
);
CREATE TABLE IF NOT EXISTS ShipMount_Deposits_relationship (
    shipMount TEXT NOT NULL REFERENCES ShipMount(symbol),
    deposit TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS ShipNav (
    systemSymbol TEXT NOT NULL REFERENCES System(symbol),
    waypointSymbol TEXT NOT NULL REFERENCES Waypoint(symbol),
    route INT NOT NULL REFERENCES ShipNavRoute(rowid),
    status TEXT NOT NULL REFERENCES ShipNavStatus(value),
    flightMode TEXT NOT NULL REFERENCES ShipNavFlightMode(value),
    route_destination INT NOT NULL REFERENCES Waypoint(rowid),
    route_departure INT NOT NULL REFERENCES Waypoint(rowid),
    route_departureTime INT NOT NULL,
    route_arrival INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ShipNavFlightMode (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS ShipNavStatus (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS ShipReactor (
    symbol TEXT NOT NULL PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    powerOutput INT NOT NULL,
    requirements INT NOT NULL UNIQUE REFERENCES ShipRequirements(rowid)
);
CREATE TABLE IF NOT EXISTS ShipRegistration(
    name TEXT NOT NULL,
    factionSymbol TEXT NOT NULL REFERENCES Faction(symbol),
    role TEXT NOT NULL REFERENCES ShipRole(value)
);
CREATE TABLE IF NOT EXISTS ShipRequirements(
    power INT,
    crew INT,
    slots INT
);
CREATE TABLE IF NOT EXISTS ShipRole (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS ShipType (
    value TEXT NOT NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS Shipyard (
    symbol TEXT NOT NULL PRIMARY KEY REFERENCES Waypoint(symbol),
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ShipYard_ShipType_relationships(
    shipYard TEXT NOT NULL REFERENCES Shipyard(symbol),
    shipType TEXT NOT NULL REFERENCES ShipType(value)
);
CREATE TABLE IF NOT EXISTS ShipyardShip (
    type TEXT REFERENCES ShipType(value),
    name TEXT NOT NULL,
    description TEXT NOT NULL,
    purchasePrice INT NOT NULL,
    frame TEXT NOT NULL REFERENCES ShipFrame(symbol),
    reactor TEXT NOT NULL REFERENCES ShipReactor(symbol),
    engine TEXT NOT NULL REFERENCES ShipEngine(symbol)
);
CREATE TABLE IF NOT EXISTS Shipyard_ShipyardShip_relationship (
    shipyardShip TEXT NOT NULL PRIMARY KEY REFERENCES ShipYardShip(type),
    shipyard TEXT NOT NULL REFERENCES Shipyard(symbol)
);
CREATE TABLE IF NOT EXISTS ShipyardShip_ShipModule_relationship (
    shipyardShip TEXT NOT NULL REFERENCES ShipyardShip(rowid),
    shipModule TEXT NOT NULL REFERENCES ShipModule(symbol)
);
CREATE TABLE IF NOT EXISTS ShipyardShip_ShipMount_relationship (
    shipyardShip TEXT NOT NULL REFERENCES ShipyardShip(rowid),
    shipMount TEXT NOT NULL REFERENCES shipMount(symbol)
);
CREATE TABLE IF NOT EXISTS ShipyardTransaction (
    waypointSymbol TEXT NOT NULL REFERENCES Waypoint(symbol),
    shipSymbol TEXT NOT NULL REFERENCES Ship(symbol),
    price INT NOT NULL,
    agentSymbol TEXT NOT NULL REFERENCES Agent(symbol),
    timestamp INT NOT NULL
);
CREATE TABLE IF NOT EXISTS ShipyardTransaction_Shipyard_relationship (
    shipyardTransaction INT NOT NULL PRIMARY KEY REFERENCES ShipyardTransaction(rowid),
    shipyard TEXT NOT NULL REFERENCES Shipyard(symbol)
);
CREATE TABLE IF NOT EXISTS Survey (
    signature TEXT NO NULL,
    symbol TEXT NO NULL REFERENCES Waypoint(symbol),
    expiration INT NO NULL,
    size TEXT NO NULL CHECK (size IN ('SMALL','MODERATE','LARGE')),
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS SurveyDeposit (
    symbol TEXT NO NULL PRIMARY KEY REFERENCES TradeSymbol(value)
);
CREATE TABLE IF NOT EXISTS SurveyDeposit_Survey_relationship (
    deposit INT NO NULL PRIMARY KEY REFERENCES SurveyDeposit(rowid),
    survey TEXT NO NULL REFERENCES Survey(signature)
);
CREATE TABLE IF NOT EXISTS System (
    symbol TEXT NO NULL PRIMARY KEY,
    sectorSymbol TEXT NO NULL,
    type TEXT NO NULL REFERENCES SystemType(value),
    x INT NO NULL,
    y INT NO NULL,
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS System_Faction_relationship(
    system TEXT NO NULL REFERENCES System(symbol),
    faction TEXT NO NULL REFERENCES Faction(symbol)
);
CREATE TABLE IF NOT EXISTS SystemType (
    value TEXT NO NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS TradeGood (
    symbol TEXT NO NULL REFERENCES TradeSymbol(value),
    name TEXT NO NULL,
    description TEXT NO NULL
);
CREATE TABLE IF NOT EXISTS TradeSymbol (
    value TEXT NO NULL PRIMARY KEY
);
CREATE TABLE IF NOT EXISTS Waypoint (
    symbol TEXT NOT NULL PRIMARY KEY,
    type TEXT NOT NULL,
    systemSymbol TEXT NOT NULL REFERENCES System(symbol),
    x INT NOT NULL,
    y INT NOT NULL,
    faction TEXT REFERENCES Faction(symbol),
    lastEdited INT NOT NULL
);
CREATE TABLE IF NOT EXISTS Orbital (
    symbol TEXT NO NULL PRIMARY KEY REFERENCES Waypoint(symbol),
    parent TEXT NO NULL REFERENCES Waypoint(symbol)
);
CREATE TABLE IF NOT EXISTS WaypointTrait (
    symbol TEXT NO NULL PRIMARY KEY,
    name TEXT NO NULL,
    description TEXT NO NULL
);
CREATE TABLE IF NOT EXISTS Waypoint_WaypointTrait_relationship (
    waypoint TEXT NO NULL REFERENCES Waypoint(symbol),
    trait TEXT NO NULL REFERENCES WaypointTrait(symbol)
);
CREATE TABLE IF NOT EXISTS WaypointType (
    value TEXT NO NULL PRIMARY KEY
);

-- Prep enums.
INSERT INTO ContractType VALUES ('PROCUREMENT'),
('TRANSPORT'),('SHUTTLE');
INSERT INTO FactionTraitSymbols VALUES ('BUREAUCRATIC'),
('SECRETIVE'),('CAPITALISTIC'),('INDUSTRIOUS'),('PEACEFUL'),
('DISTRUSTFUL'),('WELCOMING'),('SMUGGLERS'),('SCAVENGERS'),
('REBELLIOUS'),('EXILES'),('PIRATES'),('RAIDERS'),('CLAN'),
('GUILD'),('DOMINION'),('FRINGE'),('FORSAKEN'),('ISOLATED'),
('LOCALIZED'),('ESTABLISHED'),('NOTABLE'),('DOMINANT'),
('INESCAPABLE'),('INNOVATIVE'),('BOLD'),('VISIONARY'),
('CURIOUS'),('DARING'),('EXPLORATORY'),('RESOURCEFUL'),
('FLEXIBLE'),('COOPERATIVE'),('UNITED'),('STRATEGIC'),
('INTELLIGEN'),('TRESEARCH_FOCUSED'),('COLLABORATIVE'),
('PROGRESSIVE'),('MILITARISTIC'),
('TECHNOLOGICALLY_ADVANCED'),('AGGRESSIVE'),
('IMPERIALISTIC'),('TREASURE_HUNTERS'),('DEXTEROUS'),
('UNPREDICTABLE'),('BRUTAL'),('FLEETING'),('ADAPTABLE'),
('SELF_SUFFICIENT'),('DEFENSIVE'),('PROUD'),('DIVERSE'),
('INDEPENDENT'),('SELF_INTERESTED'),('FRAGMENTED'),
('COMMERCIAL'),('FREE_MARKETS'),('ENTREPRENEURIAL');
INSERT INTO MarketTradeGoodSupply (value) VALUES ('SCARCE'),
('LIMITED'),('MODERATE'),('ABUNDANT');
INSERT INTO MarketTransactionType (value) VALUES
('PURCHASE'),('SELL');
INSERT INTO ShipCrewRotation (value) VALUES ('STRICT'),
('RELAXED');
INSERT INTO ShipNavFlightMode(value) VALUES ('DRIFT'),
('STEALTH'),('CRUISE'),('BURN');
INSERT INTO ShipNavStatus(value) VALUES ('IN_TRANSIT'),
('IN_ORBIT'),('DOCKED');
INSERT INTO ShipRole (value) VALUES ('FABRICATOR'),
('HARVESTER'),('HAULER'),('INTERCEPTOR'),('EXCAVATOR'),
('TRANSPORT'),('REPAIR'),('SURVEYOR'),('COMMAND'),
('CARRIER'),('PATROL'),('SATELLITE'),('EXPLORER'),
('REFINERY');
INSERT INTO ShipType (value) VALUES ('SHIP_PROBE'),
('SHIP_MINING_DRONE'),('SHIP_INTERCEPTOR'),
('SHIP_LIGHT_HAULER'),('SHIP_COMMAND_FRIGATE'),
('SHIP_EXPLORER'),('SHIP_HEAVY_FREIGHTER'),
('SHIP_LIGHT_SHUTTLE'),('SHIP_ORE_HOUND'),
('SHIP_REFINING_FREIGHTER');
INSERT INTO SystemType (value) VALUES ('NEUTRON_STAR'),
('RED_STAR'),('ORANGE_STAR'),('BLUE_STAR'),('YOUNG_STAR'),
('WHITE_DWARF'),('BLACK_HOLE'),('HYPERGIANT'),('NEBULA'),
('UNSTABLE');
INSERT INTO TradeSymbol (value) VALUES ('PRECIOUS_STONES'),
('QUARTZ_SAND'),('SILICON_CRYSTALS'),('AMMONIA_ICE'),
('LIQUID_HYDROGEN'),('LIQUID_NITROGEN'),('ICE_WATER'),
('EXOTIC_MATTER'),('ADVANCED_CIRCUITRY'),
('GRAVITON_EMITTERS'),('IRON'),('IRON_ORE'),('COPPER'),
('COPPER_ORE'),('ALUMINUM'),('ALUMINUM_ORE'),('SILVER'),
('SILVER_ORE'),('GOLD'),('GOLD_ORE'),('PLATINUM'),
('PLATINUM_ORE'),('DIAMONDS'),('URANITE'),('URANITE_ORE'),
('MERITIUM'),('MERITIUM_ORE'),('HYDROCARBON'),
('ANTIMATTER'),('FERTILIZERS'),('FABRICS'),('FOOD'),
('JEWELRY'),('MACHINERY'),('FIREARMS'),('ASSAULT_RIFLES'),
('MILITARY_EQUIPMENT'),('EXPLOSIVES'),('LAB_INSTRUMENTS'),
('AMMUNITION'),('ELECTRONICS'),('SHIP_PLATING'),
('EQUIPMENT'),('FUEL'),('MEDICINE'),('DRUGS'),('CLOTHING'),
('MICROPROCESSORS'),('PLASTICS'),('POLYNUCLEOTIDES'),
('BIOCOMPOSITES'),('NANOBOTS'),('AI_MAINFRAMES'),
('QUANTUM_DRIVES'),('ROBOTIC_DRONES'),('CYBER_IMPLANTS'),
('GENE_THERAPEUTICS'),('NEURAL_CHIPS'),('MOOD_REGULATORS'),
('VIRAL_AGENTS'),('MICRO_FUSION_GENERATORS'),
('SUPERGRAINS'),('LASER_RIFLES'),('HOLOGRAPHICS'),
('SHIP_SALVAGE'),('RELIC_TECH'),('NOVEL_LIFEFORMS'),
('BOTANICAL_SPECIMENS'),('CULTURAL_ARTIFACTS'),
('REACTOR_SOLAR_I'),('REACTOR_FUSION_I'),
('REACTOR_FISSION_I'),('REACTOR_CHEMICAL_I'),
('REACTOR_ANTIMATTER_I'),('ENGINE_IMPULSE_DRIVE_I'),
('ENGINE_ION_DRIVE_I'),('ENGINE_ION_DRIVE_II'),
('ENGINE_HYPER_DRIVE_I'),('MODULE_MINERAL_PROCESSOR_I'),
('MODULE_CARGO_HOLD_I'),('MODULE_CREW_QUARTERS_I'),
('MODULE_ENVOY_QUARTERS_I'),('MODULE_PASSENGER_CABIN_I'),
('MODULE_MICRO_REFINERY_I'),('MODULE_ORE_REFINERY_I'),
('MODULE_FUEL_REFINERY_I'),('MODULE_SCIENCE_LAB_I'),
('MODULE_JUMP_DRIVE_I'),('MODULE_JUMP_DRIVE_II'),
('MODULE_JUMP_DRIVE_III'),('MODULE_WARP_DRIVE_I'),
('MODULE_WARP_DRIVE_II'),('MODULE_WARP_DRIVE_III'),
('MODULE_SHIELD_GENERATOR_I'),
('MODULE_SHIELD_GENERATOR_II'),('MOUNT_GAS_SIPHON_I'),
('MOUNT_GAS_SIPHON_II'),('MOUNT_GAS_SIPHON_III'),
('MOUNT_SURVEYOR_I'),('MOUNT_SURVEYOR_II'),
('MOUNT_SURVEYOR_III'),('MOUNT_SENSOR_ARRAY_I'),
('MOUNT_SENSOR_ARRAY_II'),('MOUNT_SENSOR_ARRAY_III'),
('MOUNT_MINING_LASER_I'),('MOUNT_MINING_LASER_II'),
('MOUNT_MINING_LASER_III'),('MOUNT_LASER_CANNON_I'),
('MOUNT_MISSILE_LAUNCHER_I'),('MOUNT_TURRET_I');
INSERT INTO WaypointType (value) VALUES ('PLANET'),
('GAS_GIANT'),('MOON'),('ORBITAL_STATION'),('JUMP_GATE'),
('ASTEROID_FIELD'),('NEBULA'),('DEBRIS_FIELD'),
('GRAVITY_WELL');
