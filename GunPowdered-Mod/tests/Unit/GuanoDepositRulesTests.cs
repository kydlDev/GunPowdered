using System;
using GunPowdered.Features.Guano.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Every branch of the guano deposit rules: the user's percentages (Project description) and ranges, plus the edge draws 0 and just below 1.
    public class GuanoDepositRulesTests
    {
        static Func<double> Fixed(params double[] draws)
        {
            int i = 0;
            return () => draws[i++];
        }

        [TestCase(0.00, 1)]
        [TestCase(0.099, 1)]
        [TestCase(0.10, 2)]
        [TestCase(0.1499999, 2)]
        [TestCase(0.1500001, 0)]   // not exactly 0.15: 0.10 + 0.05 is stored as 0.15000000000000002, a draw cannot tell the two apart
        [TestCase(0.99, 0)]
        public void TrollCaveOrBurialChamber_DepositCount_FollowsTenAndFivePercent(double draw, int expected)
        {
            Assert.That(GuanoDepositRules.DepositCount(CaveKind.TrollCaveOrBurialChamber, Fixed(draw)), Is.EqualTo(expected));
        }

        [TestCase(0.00, 1)]
        [TestCase(0.049, 1)]
        [TestCase(0.05, 2)]
        [TestCase(0.099, 2)]
        [TestCase(0.10, 3)]
        [TestCase(0.1499999, 3)]
        [TestCase(0.1500001, 0)]
        public void MountainCave_DepositCount_IsFifteenPercentSplitEvenly(double draw, int expected)
        {
            Assert.That(GuanoDepositRules.DepositCount(CaveKind.MountainCave, Fixed(draw)), Is.EqualTo(expected));
        }

        [TestCase(0.00, 1)]
        [TestCase(0.0333, 1)]
        [TestCase(0.034, 2)]
        [TestCase(0.0666, 2)]
        [TestCase(0.067, 3)]
        [TestCase(0.0999, 3)]
        [TestCase(0.10, 0)]
        public void SwampBatArea_DepositCount_IsTenPercentSplitEvenly(double draw, int expected)
        {
            Assert.That(GuanoDepositRules.DepositCount(CaveKind.SwampBatArea, Fixed(draw)), Is.EqualTo(expected));
        }

        [TestCase(CaveKind.TrollCaveOrBurialChamber, 0.0, 1)]
        [TestCase(CaveKind.TrollCaveOrBurialChamber, 0.99, 2)]
        [TestCase(CaveKind.MountainCave, 0.0, 1)]
        [TestCase(CaveKind.MountainCave, 0.99, 4)]
        [TestCase(CaveKind.SwampBatArea, 0.0, 1)]
        [TestCase(CaveKind.SwampBatArea, 0.99, 2)]
        public void GatherablesPerDeposit_StaysInTheKindsRange(CaveKind kind, double draw, int expected)
        {
            Assert.That(GuanoDepositRules.GatherablesPerDeposit(kind, Fixed(draw)), Is.EqualTo(expected));
        }

        [Test]
        public void MaxDeposits_AndMaxGatherables_MatchTheDescription()
        {
            Assert.That(GuanoDepositRules.MaxDeposits(CaveKind.TrollCaveOrBurialChamber), Is.EqualTo(2));
            Assert.That(GuanoDepositRules.MaxDeposits(CaveKind.MountainCave), Is.EqualTo(3));
            Assert.That(GuanoDepositRules.MaxDeposits(CaveKind.SwampBatArea), Is.EqualTo(3));
            Assert.That(GuanoDepositRules.MaxGatherables(CaveKind.MountainCave), Is.EqualTo(4));
            Assert.That(GuanoDepositRules.MaxGatherables(CaveKind.TrollCaveOrBurialChamber), Is.EqualTo(2));
        }

        [Test]
        public void Plan_NoDeposit_IsEmptyAndDrawsOnlyOnce()
        {
            int draws = 0;
            int[] plan = GuanoDepositRules.Plan(CaveKind.MountainCave, () => { draws++; return 0.9; });
            Assert.That(plan, Is.Empty);
            Assert.That(draws, Is.EqualTo(1));
        }

        [Test]
        public void Plan_TwoDeposits_HasOneAmountPerDeposit()
        {
            // 0.12 -> two deposits in a Troll Cave; then 0.0 -> 1 gatherable, 0.99 -> 2 gatherables
            int[] plan = GuanoDepositRules.Plan(CaveKind.TrollCaveOrBurialChamber, Fixed(0.12, 0.0, 0.99));
            Assert.That(plan, Is.EqualTo(new[] { 1, 2 }));
        }

        [TestCase(CaveKind.TrollCaveOrBurialChamber)]
        [TestCase(CaveKind.MountainCave)]
        [TestCase(CaveKind.SwampBatArea)]
        public void Plan_OverManyDraws_NeverLeavesTheRanges(CaveKind kind)
        {
            var random = new Random(12345);
            for (int i = 0; i < 20000; i++)
            {
                int[] plan = GuanoDepositRules.Plan(kind, random.NextDouble);
                Assert.That(plan.Length, Is.LessThanOrEqualTo(GuanoDepositRules.MaxDeposits(kind)));
                foreach (int amount in plan)
                {
                    Assert.That(amount, Is.InRange(1, GuanoDepositRules.MaxGatherables(kind)));
                }
            }
        }

        [TestCase(CaveKind.TrollCaveOrBurialChamber, 0.15)]
        [TestCase(CaveKind.MountainCave, 0.15)]
        [TestCase(CaveKind.SwampBatArea, 0.10)]
        public void DepositCount_OverManyDraws_HitsTheStatedShareOfCaves(CaveKind kind, double expectedShare)
        {
            var random = new Random(777);
            const int Draws = 200000;
            int withDeposits = 0;
            for (int i = 0; i < Draws; i++)
            {
                if (GuanoDepositRules.DepositCount(kind, random.NextDouble) > 0) withDeposits++;
            }
            Assert.That((double)withDeposits / Draws, Is.EqualTo(expectedShare).Within(0.005));
        }
    }
}
