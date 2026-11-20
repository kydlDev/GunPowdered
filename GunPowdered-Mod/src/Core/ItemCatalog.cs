using System.Collections.Generic;

namespace GunPowdered.Core
{
    /// <summary>What kind of game object a catalog entry becomes.</summary>
    public enum ItemKind
    {
        /// A stackable material (ItemDrop, Material type).
        Material,
        /// A one-handed tool or bucket the player holds.
        Tool,
        /// A crafting station or container piece the player builds with the hammer.
        Piece,
        /// A deposit placed in the world by the mod (not craftable, not in the inventory).
        Deposit,
        /// A weapon or thrown explosive the player holds and attacks with (D-045).
        Weapon
    }

    /// <summary>One cost line: a vanilla or mod prefab name and an amount.</summary>
    public sealed class CostLine
    {
        public CostLine(string prefab, int amount)
        {
            if (RuleLog.On) RuleLog.Verbose($"CostLine.CostLine(prefab={RuleLog.Shown(prefab)}, amount={RuleLog.Shown(amount)})");
            Prefab = prefab;
            Amount = amount;
        }

        public string Prefab { get; }
        public int Amount { get; }
    }

    /// <summary>
    /// Everything the mod adds, as data (D-013, D-014). Pure: no Valheim types, so the unit tests check it. <see cref="CloneOf"/> is the vanilla prefab the
    /// placeholder is cloned from until the real model comes from Unity / Blender; the Jotunn adapters turn each entry into a game object.
    /// </summary>
    public sealed class ItemSpec
    {
        public ItemSpec(string id, string name, string description, ItemKind kind, string cloneOf, int stack, float weight, string station, int yield, params CostLine[] cost)
        {
            if (RuleLog.On) RuleLog.Verbose($"ItemSpec.ItemSpec(id={RuleLog.Shown(id)}, name={RuleLog.Shown(name)}, description={RuleLog.Shown(description)}, kind={RuleLog.Shown(kind)}, cloneOf={RuleLog.Shown(cloneOf)}, stack={RuleLog.Shown(stack)}, weight={RuleLog.Shown(weight)}, station={RuleLog.Shown(station)}, yield={RuleLog.Shown(yield)})");
            Id = id;
            Name = name;
            Description = description;
            Kind = kind;
            CloneOf = cloneOf;
            Stack = stack;
            Weight = weight;
            Station = station;
            Yield = yield;
            Cost = cost;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ItemKind Kind { get; }
        public string CloneOf { get; }
        public int Stack { get; }
        public float Weight { get; }
        /// <summary>Crafting station (vanilla or GP_) the recipe needs; empty = no recipe for this entry.</summary>
        public string Station { get; }
        /// <summary>How many items one craft gives.</summary>
        public int Yield { get; }
        public IList<CostLine> Cost { get; }
        public bool HasRecipe { get { return !string.IsNullOrEmpty(Station); } }
        /// <summary>The level the crafting station needs for the recipe (1 = any).</summary>
        public int StationLevel { get; private set; } = 1;

        /// <summary>A world (global) key the recipe waits for, e.g. "defeated_fader"; null = none. Until the key is set the recipe stays hidden (RecipeGate).</summary>
        public string RequiredGlobalKey { get; private set; }

        /// <summary>Hides the recipe until the world has the global key. Expected outcome: this entry, with <see cref="RequiredGlobalKey"/> = key.</summary>
        public ItemSpec AfterGlobalKey(string key)
        {
            if (RuleLog.On) RuleLog.Verbose($"ItemSpec.AfterGlobalKey(key={RuleLog.Shown(key)})");
            RequiredGlobalKey = string.IsNullOrEmpty(key) ? null : key;
            { var returnedAt79 = this; if (RuleLog.On) RuleLog.Verbose($"ItemSpec.AfterGlobalKey: returns {RuleLog.Shown(returnedAt79)} (line 79)"); return returnedAt79; }
        }

        /// <summary>Sets the station level the recipe needs. Expected outcome: this entry, with <see cref="StationLevel"/> = level (at least 1).</summary>
        public ItemSpec AtStationLevel(int level)
        {
            if (RuleLog.On) RuleLog.Verbose($"ItemSpec.AtStationLevel(level={RuleLog.Shown(level)})");
            StationLevel = level < 1 ? 1 : level;
            { var returnedAt87 = this; if (RuleLog.On) RuleLog.Verbose($"ItemSpec.AtStationLevel: returns {RuleLog.Shown(returnedAt87)} (line 87)"); return returnedAt87; }
        }
    }

    /// <summary>The balance table the user approved (D-014) and the placeholder bases. Numbers are defaults; the Jotunn adapters read the Tunables.</summary>
    public static class ItemCatalog
    {
        public const string Workbench = "piece_workbench";
        public const string Forge = "forge";
        /// <summary>Vanilla prefab names of the materials the recipes use (Charcoal is the prefab "Coal", Sulfur is the prefab "SulfurStone"; checked in the Unity rip 2026-11-17).</summary>
        public const string Wood = "Wood", Iron = "Iron", Bronze = "Bronze", LeatherScraps = "LeatherScraps", Resin = "Resin", Stone = "Stone", Charcoal = "Coal", Sulfur = "SulfurStone", Obsidian = "Obsidian", LoxPelt = "LoxPelt", SealHide = "SealHide";

        /// <summary>The world key Fader's death sets (Ashlands boss); the Deep North Dynamite Pack waits for it.</summary>
        public const string FaderKey = "defeated_fader";

        public static readonly ItemSpec BatGuano = new ItemSpec(GunPowderedPrefabs.BatGuano, "Bat Guano", "Dung of cave bats. Rich in nitrates: leach it with water in a Leaching Vat to get saltpeter.",
            ItemKind.Material, "Resin", 50, 0.5f, "", 1);

        public static readonly ItemSpec Saltpeter = new ItemSpec(GunPowderedPrefabs.Saltpeter, "Saltpeter", "A white powder leached from bat guano. One part of gunpowder.",
            ItemKind.Material, "Flint", 50, 0.3f, "", 1);

        public static readonly ItemSpec Gunpowder = new ItemSpec(GunPowderedPrefabs.Gunpowder, "Gunpowder", "Black powder ground from saltpeter, charcoal and sulfur. Handle with care.",
            ItemKind.Material, "Coal", 50, 0.3f, GunPowderedPrefabs.PowderMill, 5,
            new CostLine(GunPowderedPrefabs.Saltpeter, 3), new CostLine(Charcoal, 1), new CostLine(Sulfur, 1));

        public static readonly ItemSpec Water = new ItemSpec(GunPowderedPrefabs.Water, "Water", "Fresh water, carried in a bucket.",
            ItemKind.Material, "Stone", 50, 0.1f, "", 1);

        public static readonly ItemSpec Bucket = new ItemSpec(GunPowderedPrefabs.Bucket, "Bucket", "A wooden bucket with bronze hoops and a leather rope. Hold it, face water and use it to fill it.",
            ItemKind.Tool, "KnifeFlint", 1, 1f, Workbench, 1,
            new CostLine(Wood, 8), new CostLine(Bronze, 1), new CostLine(LeatherScraps, 2));

        public static readonly ItemSpec BucketFull = new ItemSpec(GunPowderedPrefabs.BucketFull, "Bucket (full)", "A bucket full of water. It empties into your inventory.",
            ItemKind.Tool, "KnifeFlint", 1, 3f, "", 1);

        public static readonly ItemSpec LeachingVat = new ItemSpec(GunPowderedPrefabs.LeachingVat, "Leaching Station", "A stone-filled barrel drips guano water into a cauldron over a hearth. Burns wood for a day, then leaves saltpeter.",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(Wood, 30), new CostLine(Stone, 15), new CostLine(Bronze, 6), new CostLine(Resin, 2));

        public static readonly ItemSpec PowderMill = new ItemSpec(GunPowderedPrefabs.PowderMill, "Powder Mill", "A small mill with a slow wheel and barrels. Grinds gunpowder.",
            ItemKind.Piece, "piece_workbench", 1, 0f, Workbench, 1,
            new CostLine(Wood, 40), new CostLine(Stone, 20), new CostLine(Bronze, 5));

        public static readonly ItemSpec RainBarrel = new ItemSpec(GunPowderedPrefabs.RainBarrel, "Rain Barrel", "Collects water while it rains. Place it under open sky.",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(Wood, 10), new CostLine(Resin, 2), new CostLine(Bronze, 1));

        public static readonly ItemSpec GuanoDeposit = new ItemSpec(GunPowderedPrefabs.GuanoDeposit, "Bat Guano", "A heap of bat guano. Dig it out with a pickaxe.",
            ItemKind.Deposit, "Pickable_SulfurRock", 1, 0f, "", 1);

        public static readonly ItemSpec SulfurousMud = new ItemSpec(GunPowderedPrefabs.SulfurousMud, "Sulfurous Mud", "Yellow mud that smells of sulfur. Mine it with a pickaxe.",
            ItemKind.Deposit, "Pickable_SulfurRock", 1, 0f, "", 1);

        // D-047 (user approved): Mountains / silver tier, Workbench level 3 like the vanilla bombs; cloned from the vanilla Ooze Bomb for its throw_bomb attack
        public static readonly ItemSpec SpikedGrenade = new ItemSpec(GunPowderedPrefabs.SpikedGrenade, "Spiked Grenade",
            "A clay pot full of gunpowder, studded with spikes. Throw it: it bursts where it lands. Hold it to see the arc.",
            ItemKind.Weapon, "BombOoze", 20, 0.5f, Workbench, 5,
            new CostLine(GunPowderedPrefabs.Gunpowder, 3), new CostLine(Obsidian, 2), new CostLine(Stone, 2), new CostLine(Resin, 1)).AtStationLevel(3);

        // D-051 / D-052 / D-053 (user answers): the mounted hand cannon, Swamp (iron) tier. The ball and the cannon are carried items that never stack and cannot be
        // equipped (Material placeholder, "cannot be shot while carried"); the stock is the build piece the cannon is mounted on (Hammer, KY-Items category).
        // The ball recipe is 2 Iron at a Forge level 2 (user, 2026-11-18).
        public static readonly ItemSpec CannonBall = new ItemSpec(GunPowderedPrefabs.CannonBall, "Cannon Ball",
            "A cast iron ball. Feed it into the muzzle of a mounted Hand Cannon together with gunpowder. It can be picked up and used again after the shot.",
            ItemKind.Material, Iron, 1, 3f, Forge, 1, new CostLine(Iron, 2)).AtStationLevel(2);

        public static readonly ItemSpec Cannon = new ItemSpec(GunPowderedPrefabs.Cannon, "Hand Cannon",
            "A heavy cast iron cannon after the old Loshult gun. It cannot be fired in the hand: build a Cannon Stock and mount it there.",
            ItemKind.Material, Iron, 1, 20f, Forge, 1,
            new CostLine(Iron, 20), new CostLine(Wood, 5), new CostLine(Resin, 5)).AtStationLevel(2);

        public static readonly ItemSpec CannonStock = new ItemSpec(GunPowderedPrefabs.CannonStock, "Cannon Stock",
            "A wooden trestle for a Hand Cannon, with the rear support lower than the front. Mount the cannon on it; dismantling gives back the cannon and the wood.",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(Wood, 10), new CostLine(Resin, 4));

        // D-076 (Claude-Agent-5, PROPOSED recipes and tiers, the user approves or changes them): the other D-045 weapons, items only for now (the behaviour comes
        // with their own stages). Like the Hand Cannon they are carried items that never stack as a weapon and cannot be equipped yet (Material placeholder).
        public const string BlackMetal = "BlackMetal", Feathers = "Feathers", Chain = "Chain", Flametal = "FlametalNew", Ashwood = "Blackwood", Arbalest = "CrossbowArbalest", IronArrow = "ArrowIron", Club = "Club", Cart = "Cart";

        // T-009c iron musket (D-045 item 3): Swamp (iron) tier like the Hand Cannon, Forge level 2. D-087: a real two-handed weapon cloned from the vanilla Arbalest (attack, reload, animation state); the musket's own clips come with D-088
        public static readonly ItemSpec Musket = new ItemSpec(GunPowderedPrefabs.Musket, "Iron Musket",
            "A long iron-barrelled gun with a matchlock and a brass-bound stock. Loaded with gunpowder and iron bullets, five shots at a time.",
            ItemKind.Weapon, Arbalest, 1, 4f, Forge, 1,
            new CostLine(Iron, 15), new CostLine(Wood, 10), new CostLine(Bronze, 2)).AtStationLevel(2);

        // T-009d explosive arrows (D-045 item 5): Plains tier (black metal) so they are no early advantage; like the vanilla arrows 10 per craft
        public static readonly ItemSpec ExplosiveArrow = new ItemSpec(GunPowderedPrefabs.ExplosiveArrow, "Explosive Arrow",
            "A black metal arrowhead with a bundle of gunpowder tied behind it. It bursts where it hits.",
            ItemKind.Weapon, IronArrow, 50, 0.1f, Forge, 10,       // real arrow ammunition for any bow (D-100), cloned from the Iron Arrow
            new CostLine(Wood, 8), new CostLine(BlackMetal, 1), new CostLine(Feathers, 2), new CostLine(GunPowderedPrefabs.Gunpowder, 2)).AtStationLevel(4);

        // T-009e dynamite (D-045 item 6): iron age, a metal tube of gunpowder lit with the Torch; for gathering, not for fighting
        public static readonly ItemSpec Dynamite = new ItemSpec(GunPowderedPrefabs.Dynamite, "Dynamite",
            "A metal tube packed with gunpowder and a long fuse. Set it down, then light it with a Torch and get away: it tears holes in rock and ground and wrecks buildings, but hurts creatures only a fifth as much.",
            ItemKind.Weapon, Club, 20, 0.5f, Forge, 5,       // held in one hand; the attack button sets it down (D-100)
            new CostLine(Iron, 2), new CostLine(GunPowderedPrefabs.Gunpowder, 5), new CostLine(Resin, 1)).AtStationLevel(2);

        // D-108 (user 2026-11-20): the Deep North Dynamite Pack - four sticks in the corners, two Spiked Grenades stacked in the middle, four SealHide straps round
        // them. Made at the dynamite's Forge (level 2) from what it is made of, only after Fader is dead. Breaks tier 6 rock (the gammel trolls) in a 20 m blast.
        public static readonly ItemSpec DeepNorthPack = new ItemSpec(GunPowderedPrefabs.DeepNorthPack, "Deep North Dynamite Pack",
            "Four sticks of dynamite and two spiked grenades strapped together with seal hide. Set it down, light it with a Torch and run: the blast reaches 20 m and breaks even the frozen gammel trolls of the Deep North.",
            ItemKind.Weapon, Club, 5, 4f, Forge, 1,
            new CostLine(GunPowderedPrefabs.Dynamite, 4), new CostLine(GunPowderedPrefabs.SpikedGrenade, 2), new CostLine(SealHide, 4)).AtStationLevel(2).AfterGlobalKey(FaderKey);

        // T-009f mobile cannon (D-045 item 4): Ashlands (Flametal + Ashwood); a build piece like the Cannon Stock, built beside a Forge
        public static readonly ItemSpec MobileCannon = new ItemSpec(GunPowderedPrefabs.MobileCannon, "Mobile Cannon",
            "A Flametal cannon on an ashwood carriage with big spoked wheels and an elevation crank. One shot is loaded at a time; an Asksvin with a harness can pull it.",
            ItemKind.Piece, Cart, 1, 0f, Forge, 1,               // the game's cart: pulled by the player (and later the Asksvin) with its own joint (D-104)
            new CostLine(Flametal, 20), new CostLine(Ashwood, 30), new CostLine(Iron, 10));

        // D-063 / D-068: the harness that hangs on an Asksvin's saddle and tows the Mobile Cannon
        public static readonly ItemSpec AsksvinHarness = new ItemSpec(GunPowderedPrefabs.AsksvinHarness, "Asksvin Harness",
            "Leather straps and a chain that hang on an Asksvin's saddle, so the rider can tow a Mobile Cannon.",
            ItemKind.Material, Chain, 1, 5f, Workbench, 1,
            new CostLine(LoxPelt, 10), new CostLine(Chain, 3), new CostLine(Flametal, 2), new CostLine(Bronze, 2)).AtStationLevel(2);

        // D-080 / D-081 (user): the musket ammunition chain. Paper (2 Resin + 1 Leather Scraps, Workbench; the user gave no yield, so 1 per craft: my reading), iron bullets
        // (1 Iron makes 5, Forge level 2), paper cartridges (1 Gunpowder + 1 bullet + 1 paper, Workbench level 2). The musket loads cartridges, never loose bullets.
        public static readonly ItemSpec Paper = new ItemSpec(GunPowderedPrefabs.Paper, "Paper",
            "Plain sheets pressed from resin and leather scraps. Wrapped round gunpowder and a bullet they make a cartridge.",
            ItemKind.Material, Feathers, 50, 0.05f, Workbench, 1,
            new CostLine(Resin, 2), new CostLine(LeatherScraps, 1));

        public static readonly ItemSpec IronBullet = new ItemSpec(GunPowderedPrefabs.IronBullet, "Iron Bullet",
            "A small cast iron ball for the Iron Musket. Wrap it with gunpowder in paper to make a cartridge.",
            ItemKind.Material, Iron, 50, 0.1f, Forge, 5, new CostLine(Iron, 1)).AtStationLevel(2);

        public static readonly ItemSpec PaperCartridge = new ItemSpec(GunPowderedPrefabs.PaperCartridge, "Cartridge",   // shown as "Cartridge" (user 2026-11-19 11:52); the prefab keeps its name
            "Gunpowder and an iron bullet wrapped in paper. Load it into the Iron Musket: the bullet flies, the paper burns away in the pan.",
            ItemKind.Material, Resin, 50, 0.15f, Workbench, 1,
            new CostLine(GunPowderedPrefabs.Gunpowder, 1), new CostLine(GunPowderedPrefabs.IronBullet, 1), new CostLine(GunPowderedPrefabs.Paper, 1)).AtStationLevel(2);

        // D-129 (user 2026-11-20): the Leaching Station extensions, one per biome after the Black Forest. Each DIFFERENT one within 5 m of a station is one
        // upgrade step (bigger batch, 5 minutes shorter run). Built with the hammer at a Workbench. Costs are Claude's proposal, the user sets the amounts.
        public const string Silver = "Silver", FineWood = "FineWood", Copper = "Copper", YggdrasilWood = "YggdrasilWood", BlackMarble = "BlackMarble";

        public static readonly ItemSpec LeachBarrel = new ItemSpec(GunPowderedPrefabs.LeachBarrel, "Second Leaching Barrel",
            "A second stone-filled barrel for a Leaching Station nearby: it takes a bigger batch and leaches faster. Leaching Station upgrade 1 (Swamp).",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(Wood, 20), new CostLine(Iron, 4), new CostLine(Resin, 4));

        public static readonly ItemSpec SettlingTank = new ItemSpec(GunPowderedPrefabs.SettlingTank, "Settling Tank",
            "A stone tank where the guano water settles before it reaches the cauldron. Leaching Station upgrade (Mountains).",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(Stone, 20), new CostLine(Silver, 4), new CostLine(Obsidian, 4));

        public static readonly ItemSpec BlackMetalBoiler = new ItemSpec(GunPowderedPrefabs.BlackMetalBoiler, "Black Metal-banded Boiler",
            "A fine wood boiler bound with black metal bands that keeps the brine hot. Leaching Station upgrade (Plains).",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(FineWood, 10), new CostLine(BlackMetal, 6));

        public static readonly ItemSpec DvergrDripPipes = new ItemSpec(GunPowderedPrefabs.DvergrDripPipes, "Dvergr Drip Pipes",
            "Copper pipes in the dvergr style that drip the brine evenly over the stones. Leaching Station upgrade (Mistlands).",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(Copper, 6), new CostLine(YggdrasilWood, 10), new CostLine(BlackMarble, 10));

        public static readonly ItemSpec FlametalHeater = new ItemSpec(GunPowderedPrefabs.FlametalHeater, "Flametal Heater",
            "A glowing flametal heater under the cauldron. Leaching Station upgrade (Ashlands): with all five the station leaches its biggest batch in 5 minutes.",
            ItemKind.Piece, "piece_chest_wood", 1, 0f, Workbench, 1,
            new CostLine(Flametal, 6), new CostLine(Ashwood, 10));

        /// <summary>The Leaching Station extensions (D-129), in biome order.</summary>
        public static IList<ItemSpec> LeachingExtensions
        {
            get { return new[] { LeachBarrel, SettlingTank, BlackMetalBoiler, DvergrDripPipes, FlametalHeater }; }
        }

        /// <summary>Every entry, in the order they are registered.</summary>
        public static IList<ItemSpec> All
        {
            get
            {
                return new[] { BatGuano, Saltpeter, Gunpowder, Water, Bucket, BucketFull, LeachingVat, PowderMill, RainBarrel, GuanoDeposit, SulfurousMud, SpikedGrenade, CannonBall, Cannon, CannonStock, Musket, ExplosiveArrow, Dynamite, MobileCannon, AsksvinHarness, Paper, IronBullet, PaperCartridge, DeepNorthPack, LeachBarrel, SettlingTank, BlackMetalBoiler, DvergrDripPipes, FlametalHeater };
            }
        }
    }
}
