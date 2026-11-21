using GunPowdered.Core;

namespace GunPowdered.Tests.Unit
{
    /// Guano and Sulfurous Mud deposits refill 2 in-game days after they were emptied (D-154).
    public class DepositRefillTests
    {
        [Test]
        public void RefillAt_IsTwoDaysAfterEmptying()
        {
            Assert.That(DepositRefill.RefillAt(1000d, 2d, 1800d), Is.EqualTo(4600d));
        }

        [Test]
        public void DefaultDays_IsTwo()
        {
            Assert.That(DepositRefill.DefaultDays, Is.EqualTo(2d));
        }

        [TestCase(4599d, false)]
        [TestCase(4600d, true)]
        [TestCase(99999d, true)]
        [TestCase(0d, false)]
        public void IsFull_OnlyFromTheRefillTime(double now, bool expected)
        {
            Assert.That(DepositRefill.IsFull(1000d, now, 2d, 1800d), Is.EqualTo(expected));
        }

        [TestCase(double.NaN, 3d, 1800d)]
        [TestCase(1000d, -1d, 1800d)]
        [TestCase(1000d, 3d, 0d)]
        [TestCase(1000d, double.NaN, 1800d)]
        public void BadInput_IsNeverFullAndHasNoWait(double emptiedAt, double days, double dayLength)
        {
            Assert.That(DepositRefill.IsFull(emptiedAt, 1e12, days, dayLength), Is.False);
            Assert.That(DepositRefill.SecondsLeft(emptiedAt, 0d, days, dayLength), Is.EqualTo(0d));
        }

        [TestCase(true, 2, 2)]
        [TestCase(true, 0, 0)]
        [TestCase(true, -1, 0)]
        [TestCase(false, 2, 0)]
        [TestCase(false, 0, 0)]
        public void Gives_OnlyAFullDepositRewards(bool full, int amount, int expected)
        {
            Assert.That(DepositRefill.Gives(full, amount), Is.EqualTo(expected));
        }

        [Test]
        public void ShowsModel_OnlyWhileFull()
        {
            Assert.That(DepositRefill.ShowsModel(true), Is.True);
            Assert.That(DepositRefill.ShowsModel(false), Is.False);
        }

        [Test]
        public void SecondsLeft_CountsDownAndStopsAtZero()
        {
            Assert.That(DepositRefill.SecondsLeft(1000d, 2000d, 2d, 1800d), Is.EqualTo(2600d));
            Assert.That(DepositRefill.SecondsLeft(1000d, 9000d, 2d, 1800d), Is.EqualTo(0d));
        }
    }
}
