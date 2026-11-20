using NUnit.Framework;
using GunPowdered.Core;
using GunPowdered.Features.Gunpowder.Domain;

namespace GunPowdered.UnitTests
{
    /// <summary>D-140 (user 2026-11-20): the 5 m bubble, extensions never on a station, side-by-side snap points, extensions snap only to their own station.</summary>
    [TestFixture]
    public class StationPlacementRulesTests
    {
        // the Powder Mill's footprint (bundle model, its own space)
        const float MinX = -1.40f, MaxX = 1.40f, MinZ = -0.82f, MaxZ = 0.82f, Top = 1.43f;
        const string Mill = "GP_PowderMill", Vat = "GP_LeachingVat";

        [TestCase(4.9f, 0f, 0f, true, TestName = "InRange_Sideways_4_9m_Counts")]
        [TestCase(0f, 4.9f, 0f, true, TestName = "InRange_Above_4_9m_Counts")]
        [TestCase(0f, -4.9f, 0f, true, TestName = "InRange_Below_4_9m_Counts")]
        [TestCase(0f, 5f, 0f, true, TestName = "InRange_TheEdgeCounts")]
        [TestCase(0f, 5.1f, 0f, false, TestName = "InRange_Above_5_1m_DoesNotCount")]
        [TestCase(3.6f, 3.6f, 0f, false, TestName = "InRange_ABubbleNotABox_DiagonalOutside")]
        public void InRange_IsASphere(float dx, float dy, float dz, bool expected)
        {
            Assert.That(StationPlacementRules.InRange(dx, dy, dz, 5f), Is.EqualTo(expected));
        }

        [Test]
        public void OnTopOfTheStation_IsOnStation()
        {
            Assert.That(StationPlacementRules.IsOnStation(0.3f, Top, 0.1f, MinX, MaxX, MinZ, MaxZ, Top), Is.True);
            Assert.That(StationPlacementRules.IsOnStation(0.3f, 0.4f, 0.1f, MinX, MaxX, MinZ, MaxZ, Top), Is.True, "on the mill's low base inside the footprint");
        }

        [Test]
        public void GluedToASide_IsNotOnStation()
        {
            Assert.That(StationPlacementRules.IsOnStation(MaxX + 0.5f, 0f, 0f, MinX, MaxX, MinZ, MaxZ, Top), Is.False);
            Assert.That(StationPlacementRules.IsOnStation(MaxX, 0.6f, 0f, MinX, MaxX, MinZ, MaxZ, Top), Is.False, "the edge itself is outside (margin)");
        }

        [Test]
        public void InsideTheFootprintAtGroundLevel_IsNoOnTopCase_TheClippingTestRefusesIt()
        {
            Assert.That(StationPlacementRules.IsOnStation(0f, 0.05f, 0f, MinX, MaxX, MinZ, MaxZ, Top), Is.False);
        }

        [Test]
        public void OnAFloorHighAboveTheStation_IsNotOnStation()
        {
            Assert.That(StationPlacementRules.IsOnStation(0f, Top + 2f, 0f, MinX, MaxX, MinZ, MaxZ, Top), Is.False);
            Assert.That(StationPlacementRules.IsOnStation(0f, Top + StationPlacementRules.AboveTopTolerance, 0f, MinX, MaxX, MinZ, MaxZ, Top), Is.True, "resting on the top still counts");
        }

        [Test]
        public void AnExtension_SnapsOnlyToItsOwnStation()
        {
            Assert.That(StationPlacementRules.SnapAllowed(Mill, Mill, null), Is.True);
            Assert.That(StationPlacementRules.SnapAllowed(Mill, Vat, null), Is.False, "a mill extension does not glue to the Leaching Station");
            Assert.That(StationPlacementRules.SnapAllowed(Mill, null, null), Is.False, "nor to walls or floors");
            Assert.That(StationPlacementRules.SnapAllowed(Mill, null, Mill), Is.False, "nor to another extension");
        }

        [Test]
        public void OtherPieces_IgnoreTheStationsAndExtensionsPoints_ButKeepTheGamesOwn()
        {
            Assert.That(StationPlacementRules.SnapAllowed(null, Mill, null), Is.False);
            Assert.That(StationPlacementRules.SnapAllowed(null, null, Vat), Is.False);
            Assert.That(StationPlacementRules.SnapAllowed(null, null, null), Is.True);
        }

        [Test]
        public void SideSnapPoints_AreTheFourSideMiddlesAtGroundLevel()
        {
            Assert.That(StationPlacementRules.SideSnapPoints(MinX, MaxX, MinZ, MaxZ), Is.EqualTo(new[] { MinX, 0f, MaxX, 0f, 0f, MinZ, 0f, MaxZ }).Within(0.0001f));
            Assert.That(StationPlacementRules.SideSnapPoints(-0.55f, 0.55f, -0.40f, 0.68f), Is.EqualTo(new[] { -0.55f, 0.14f, 0.55f, 0.14f, 0f, -0.40f, 0f, 0.68f }).Within(0.0001f), "an off-centre model (the sieve's chute)");
        }

        [TestCase(0, 10d)]
        [TestCase(1, 7.5d)]
        [TestCase(2, 5d)]
        [TestCase(3, 2.5d)]
        [TestCase(4, 2.5d)]
        public void MillMinutes_ShrinkByTwoAndAHalfPerExtension_CappedAtThree(int kinds, double minutes)
        {
            var found = new System.Collections.Generic.List<string> { "GP_MillGrindstone", "GP_MillSieve", "GP_MillOven", "GP_Extra" }.GetRange(0, kinds);
            int step = MillUpgradeRules.Step(found);
            Assert.That(MillUpgradeRules.Minutes(step, MillUpgradeRules.DefaultBaseMinutes, MillUpgradeRules.DefaultMinutesPerStep), Is.EqualTo(minutes).Within(1e-9));
        }
    }
}
