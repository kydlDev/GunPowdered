using GunPowdered.Features.Saltpeter.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Leaching Station upgrades (D-129): step from the extensions, batch size, fill per use, whole base units, missing text and run time.
    public class LeachingUpgradeRulesTests
    {
        static readonly LeachingVatRules.VatAmounts Unit = new LeachingVatRules.VatAmounts(10, 10, 4);

        static LeachingVatRules.VatAmounts A(int guano, int water, int wood) => new LeachingVatRules.VatAmounts(guano, water, wood);

        static string T(LeachingVatRules.VatAmounts a) => LeachingUpgradeRules.Text(a);

        [Test]
        public void Step_CountsDifferentKindsOnly()
        {
            Assert.That(LeachingUpgradeRules.Step(null), Is.EqualTo(0));
            Assert.That(LeachingUpgradeRules.Step(new string[0]), Is.EqualTo(0));
            Assert.That(LeachingUpgradeRules.Step(new[] { "barrel", "barrel", "", null }), Is.EqualTo(1));
            Assert.That(LeachingUpgradeRules.Step(new[] { "barrel", "tank", "boiler" }), Is.EqualTo(3));
        }

        [Test]
        public void Step_NeverAboveFive()
        {
            Assert.That(LeachingUpgradeRules.Step(new[] { "a", "b", "c", "d", "e", "f", "g" }), Is.EqualTo(LeachingUpgradeRules.MaxStep));
        }

        [TestCase(0, "10/10/4")]
        [TestCase(1, "20/20/8")]
        [TestCase(2, "30/30/12")]
        [TestCase(3, "40/40/16")]
        [TestCase(4, "50/50/20")]
        [TestCase(5, "60/60/24")]
        [TestCase(9, "60/60/24")]
        [TestCase(-3, "10/10/4")]
        public void FullBatch_GrowsOneUnitPerStep_FuelAtTheSameRate(int step, string expected)
        {
            Assert.That(T(LeachingUpgradeRules.FullBatch(Unit, step)), Is.EqualTo(expected));
        }

        [TestCase(0, "5/5/2")]
        [TestCase(1, "10/10/4")]
        [TestCase(2, "15/15/6")]
        [TestCase(5, "30/30/12")]
        public void FillPerUse_IsHalfTheFullBatch(int step, string expected)
        {
            Assert.That(T(LeachingUpgradeRules.FillPerUse(Unit, step)), Is.EqualTo(expected));
        }

        [Test]
        public void FillPerUse_RoundsAnOddHalfUp()
        {
            Assert.That(T(LeachingUpgradeRules.FillPerUse(A(5, 3, 1), 0)), Is.EqualTo("3/2/1"));
        }

        [Test]
        public void Take_IsLimitedByTheFillPerUse()
        {
            LeachingVatRules.VatAmounts take = LeachingUpgradeRules.Take(A(0, 0, 0), A(10, 10, 4), A(5, 5, 2), A(99, 99, 99));
            Assert.That(T(take), Is.EqualTo("5/5/2"));
        }

        [Test]
        public void Take_IsLimitedByWhatThePlayerHas()
        {
            LeachingVatRules.VatAmounts take = LeachingUpgradeRules.Take(A(0, 0, 0), A(10, 10, 4), A(5, 5, 2), A(3, 0, 7));
            Assert.That(T(take), Is.EqualTo("3/0/2"));
        }

        [Test]
        public void Take_IsLimitedByTheRoomLeft()
        {
            LeachingVatRules.VatAmounts take = LeachingUpgradeRules.Take(A(8, 10, 12), A(10, 10, 4), A(5, 5, 2), A(99, 99, 99));
            Assert.That(T(take), Is.EqualTo("2/0/0"));
        }

        [Test]
        public void Take_BadInputGivesNothingNegative()
        {
            LeachingVatRules.VatAmounts take = LeachingUpgradeRules.Take(A(-5, 0, 0), A(10, 10, 4), A(5, -5, 2), A(99, 99, -1));
            Assert.That(T(take), Is.EqualTo("5/0/0"));
        }

        [TestCase(10, 10, 4, 0, 1)]
        [TestCase(15, 15, 6, 2, 1)]      // the user's example: 15 + 15 + 6 leaches one base unit, 5 + 5 + 2 stay
        [TestCase(30, 30, 12, 2, 3)]
        [TestCase(30, 30, 12, 1, 2)]     // the step allows only 2 units
        [TestCase(60, 20, 24, 5, 2)]     // water is the scarcest
        [TestCase(60, 60, 7, 5, 1)]      // wood is the scarcest
        [TestCase(9, 10, 4, 0, 0)]
        [TestCase(0, 0, 0, 5, 0)]
        public void Units_AreWholeBaseUnitsOnly(int guano, int water, int wood, int step, int expected)
        {
            Assert.That(LeachingUpgradeRules.Units(A(guano, water, wood), Unit, step), Is.EqualTo(expected));
        }

        [TestCase(0, 10, 4)]
        [TestCase(10, 0, 4)]
        [TestCase(10, 10, 0)]
        public void Units_BadUnitMakesNothing(int guano, int water, int wood)
        {
            Assert.That(LeachingUpgradeRules.Units(A(60, 60, 24), A(guano, water, wood), 5), Is.EqualTo(0));
        }

        [Test]
        public void Used_IsTheUnitTimesTheCount()
        {
            Assert.That(T(LeachingUpgradeRules.Used(Unit, 3)), Is.EqualTo("30/30/12"));
            Assert.That(T(LeachingUpgradeRules.Used(Unit, 0)), Is.EqualTo("0/0/0"));
            Assert.That(T(LeachingUpgradeRules.Used(Unit, -2)), Is.EqualTo("0/0/0"));
        }

        [Test]
        public void Remainder_AfterARun_StaysForTheNextRun()
        {
            LeachingVatRules.VatAmounts have = A(15, 15, 6);
            LeachingVatRules.VatAmounts used = LeachingUpgradeRules.Used(Unit, LeachingUpgradeRules.Units(have, Unit, 2));
            Assert.That(T(A(have.Guano - used.Guano, have.Water - used.Water, have.Fuel - used.Fuel)), Is.EqualTo("5/5/2"));
        }

        [Test]
        public void IsFull_OnlyWhenEveryMaterialReachedTheFullBatch()
        {
            Assert.That(LeachingUpgradeRules.IsFull(A(20, 20, 8), A(20, 20, 8)), Is.True);
            Assert.That(LeachingUpgradeRules.IsFull(A(20, 20, 7), A(20, 20, 8)), Is.False);
            Assert.That(LeachingUpgradeRules.IsFull(A(0, 0, 0), A(0, 0, 0)), Is.False);
        }

        [Test]
        public void Missing_NamesExactlyWhatIsShort()
        {
            Assert.That(LeachingUpgradeRules.Missing(A(10, 10, 4), Unit), Is.Empty);
            Assert.That(LeachingUpgradeRules.Missing(A(30, 7, 9), Unit), Is.EqualTo("Needs 3 more Water"));
            Assert.That(LeachingUpgradeRules.Missing(A(10, 7, 2), Unit), Is.EqualTo("Needs 3 more Water and 2 more Wood"));
            Assert.That(LeachingUpgradeRules.Missing(A(5, 5, 2), Unit), Is.EqualTo("Needs 5 more Bat Guano, 5 more Water and 2 more Wood"));
            Assert.That(LeachingUpgradeRules.Missing(A(-1, 10, 4), Unit), Is.EqualTo("Needs 10 more Bat Guano"));
        }

        [TestCase(0, 30d)]
        [TestCase(1, 25d)]
        [TestCase(2, 20d)]
        [TestCase(3, 15d)]
        [TestCase(4, 10d)]
        [TestCase(5, 5d)]
        [TestCase(8, 5d)]
        [TestCase(-1, 30d)]
        public void Minutes_FiveLessPerStep_LastStepFive(int step, double expected)
        {
            Assert.That(LeachingUpgradeRules.Minutes(step, LeachingUpgradeRules.DefaultBaseMinutes, LeachingUpgradeRules.DefaultMinutesPerStep), Is.EqualTo(expected));
        }

        [Test]
        public void Minutes_NeverBelowTheMinimum()
        {
            Assert.That(LeachingUpgradeRules.Minutes(5, 10d, 5d), Is.EqualTo(LeachingUpgradeRules.MinMinutes));
            Assert.That(LeachingUpgradeRules.Minutes(2, 30d, -5d), Is.EqualTo(30d));
        }
    }
}
