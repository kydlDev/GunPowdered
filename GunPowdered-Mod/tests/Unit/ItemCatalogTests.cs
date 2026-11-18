using System.Linq;
using GunPowdered.Core;

namespace GunPowdered.Tests.Unit
{
    /// The catalog is the approved balance table (D-014): ids, stacks and recipes must match it.
    public class ItemCatalogTests
    {
        [Test]
        public void EveryId_IsUnique_AndHasTheGpPrefix()
        {
            var ids = ItemCatalog.All.Select(i => i.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
            Assert.That(ids.All(i => i.StartsWith("GP_")), Is.True);
        }

        [Test]
        public void EveryEntry_HasANameAndAPlaceholderBase()
        {
            foreach (var i in ItemCatalog.All)
            {
                Assert.That(string.IsNullOrWhiteSpace(i.Name), Is.False, i.Id);
                Assert.That(string.IsNullOrWhiteSpace(i.CloneOf), Is.False, i.Id);
            }
        }

        [Test]
        public void Materials_StackFifty()
        {
            foreach (var i in new[] { ItemCatalog.BatGuano, ItemCatalog.Saltpeter, ItemCatalog.Gunpowder, ItemCatalog.Water })
                Assert.That(i.Stack, Is.EqualTo(50), i.Id);
        }

        [Test]
        public void RecipeEntries_HaveCostsAndAYield_OthersHaveNone()
        {
            foreach (var i in ItemCatalog.All)
            {
                if (i.HasRecipe)
                {
                    Assert.That(i.Cost.Count, Is.GreaterThan(0), i.Id);
                    Assert.That(i.Cost.All(c => c.Amount > 0 && !string.IsNullOrEmpty(c.Prefab)), Is.True, i.Id);
                    Assert.That(i.Yield, Is.GreaterThan(0), i.Id);
                }
                else Assert.That(i.Cost.Count, Is.EqualTo(0), i.Id);
            }
        }

        [Test]
        public void Gunpowder_IsThreeSaltpeterOneCharcoalOneSulfur_ForFive()
        {
            var g = ItemCatalog.Gunpowder;
            Assert.That(g.Yield, Is.EqualTo(5));
            Assert.That(g.Station, Is.EqualTo(GunPowderedPrefabs.PowderMill));
            Assert.That(g.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "GP_Saltpeter:3", "Coal:1", "SulfurStone:1" }));
        }

        [Test]
        public void Stations_CostWhatD014Says()
        {
            Assert.That(ItemCatalog.LeachingVat.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "Wood:30", "Stone:15", "Bronze:6", "Resin:2" }));
            Assert.That(ItemCatalog.PowderMill.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "Wood:40", "Stone:20", "Bronze:5" }));
            Assert.That(ItemCatalog.RainBarrel.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "Wood:10", "Resin:2", "Bronze:1" }));
            Assert.That(ItemCatalog.Bucket.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "Wood:8", "Bronze:1", "LeatherScraps:2" }));
            }

        [Test]
        public void HandCannon_Items_AreTheApprovedOnes()
        {
            // D-051 addendum 2: cannon 20 Iron + 5 Wood + 5 Resin at a Forge level 2; D-053: carried items never stack
            var cannon = ItemCatalog.Cannon;
            Assert.That(cannon.Station, Is.EqualTo(ItemCatalog.Forge));
            Assert.That(cannon.StationLevel, Is.EqualTo(2));
            Assert.That(cannon.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "Iron:20", "Wood:5", "Resin:5" }));
            Assert.That(cannon.Stack, Is.EqualTo(1));
            Assert.That(cannon.Kind, Is.EqualTo(ItemKind.Material), "a Material cannot be equipped, so it cannot be shot in the hand");

            var ball = ItemCatalog.CannonBall;
            Assert.That(ball.Station, Is.EqualTo(ItemCatalog.Forge));
            Assert.That(ball.StationLevel, Is.EqualTo(2), "user 2026-11-18: the ball is made at a Forge level 2");
            Assert.That(ball.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "Iron:2" }));
            Assert.That(ball.Stack, Is.EqualTo(1));
        }

        [Test]
        public void CannonStock_IsABuildPiece_OfTenWoodAndFourResin_AtTheWorkbench()
        {
            var stock = ItemCatalog.CannonStock;
            Assert.That(stock.Kind, Is.EqualTo(ItemKind.Piece));
            Assert.That(stock.Station, Is.EqualTo(ItemCatalog.Workbench));
            Assert.That(stock.Cost.Select(c => c.Prefab + ":" + c.Amount), Is.EquivalentTo(new[] { "Wood:10", "Resin:4" }));
        }

        [Test]
        public void ConstantsCoverEveryPrefabName()
        {
            var catalog = ItemCatalog.All.Select(i => i.Id).ToList();
            var consts = typeof(GunPowderedPrefabs).GetFields().Select(f => (string)f.GetRawConstantValue()).ToList();
            Assert.That(catalog, Is.EquivalentTo(consts));
        }
    }
}
