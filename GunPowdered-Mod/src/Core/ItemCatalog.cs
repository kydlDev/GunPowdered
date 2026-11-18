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
        Deposit
    }

    /// <summary>One cost line: a vanilla or mod prefab name and an amount.</summary>
    public sealed class CostLine
    {
        public CostLine(string prefab, int amount)
        {
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
    }

    /// <summary>The balance table the user approved (D-014) and the placeholder bases. Numbers are defaults; the Jotunn adapters read the Tunables.</summary>
    public static class ItemCatalog
    {
        public const string Workbench = "piece_workbench";
        public const string Forge = "forge";
        /// <summary>Vanilla prefab names of the materials the recipes use (Charcoal is the prefab "Coal", Sulfur is the prefab "SulfurStone"; checked in the Unity rip 2026-11-17).</summary>
        public const string Wood = "Wood", Bronze = "Bronze", LeatherScraps = "LeatherScraps", Resin = "Resin", Stone = "Stone", Charcoal = "Coal", Sulfur = "SulfurStone";

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

        /// <summary>Every entry, in the order they are registered.</summary>
        public static IList<ItemSpec> All
        {
            get
            {
                return new[] { BatGuano, Saltpeter, Gunpowder, Water, Bucket, BucketFull, LeachingVat, PowderMill, RainBarrel, GuanoDeposit, SulfurousMud };
            }
        }
    }
}
