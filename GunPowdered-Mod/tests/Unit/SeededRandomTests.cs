using System;
using GunPowdered.Core;

namespace GunPowdered.Tests.Unit
{
    /// The seeded generator of the placers (D-043): same seed = same placement on every client, numbers always in range.
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_GivesTheSameSequence()
        {
            var a = new SeededRandom(1234);
            var b = new SeededRandom(1234);
            for (int i = 0; i < 100; i++) Assert.That(a.NextDouble(), Is.EqualTo(b.NextDouble()));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            int same = 0;
            for (int i = 0; i < 100; i++) if (a.Next(1000000) == b.Next(1000000)) same++;
            Assert.That(same, Is.LessThan(5));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void NextDouble_StaysInsideZeroToOne(int seed)
        {
            var r = new SeededRandom(seed);
            for (int i = 0; i < 10000; i++)
            {
                double d = r.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0d).And.LessThan(1d));
            }
        }

        [Test]
        public void NextDouble_IsSpreadOverTheRange()
        {
            var r = new SeededRandom(42);
            int low = 0, n = 10000;
            for (int i = 0; i < n; i++) if (r.NextDouble() < 0.5d) low++;
            Assert.That(low, Is.InRange(4700, 5300));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(10)]
        public void Next_StaysInsideTheRange_AndHitsEveryValue(int max)
        {
            var r = new SeededRandom(7);
            var seen = new bool[max];
            for (int i = 0; i < 2000; i++)
            {
                int v = r.Next(max);
                Assert.That(v, Is.InRange(0, max - 1));
                seen[v] = true;
            }
            Assert.That(seen, Is.All.True);
        }

        [Test]
        public void Next_OfTheLargestInt_StaysBelowIt()
        {
            var r = new SeededRandom(99);
            for (int i = 0; i < 10000; i++) Assert.That(r.Next(int.MaxValue), Is.InRange(0, int.MaxValue - 1));
        }

        [Test]
        public void Next_OfZero_IsZero()
        {
            Assert.That(new SeededRandom(5).Next(0), Is.Zero);
        }

        [Test]
        public void Next_OfANegativeNumber_Throws()
        {
            Assert.That(() => new SeededRandom(5).Next(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void KnownSeed_GivesTheKnownFirstDraw()
        {
            // pins the algorithm (SplitMix64, values from a reference implementation): changing it moves where new deposits go, so it must be on purpose
            Assert.That(new SeededRandom(0).NextDouble(), Is.EqualTo(0.8833108082136426d).Within(1e-15));
            Assert.That(new SeededRandom(1234).NextDouble(), Is.EqualTo(0.8718981469029536d).Within(1e-15));
        }
    }
}
