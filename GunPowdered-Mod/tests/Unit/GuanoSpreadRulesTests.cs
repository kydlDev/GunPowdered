using System;
using System.Collections.Generic;
using System.Linq;
using GunPowdered.Features.Guano.Domain;

namespace GunPowdered.Tests.Unit
{
    /// D-151: a cave's deposits are dealt evenly over its floors.
    public class GuanoSpreadRulesTests
    {
        static Func<double> Zero => () => 0d;

        [Test]
        public void Floors_GroupsByHeightGaps_LowestFirst()
        {
            var heights = new List<double> { 30, 10, 11, 31, 12, 50 };   // floors: {10,11,12} {30,31} {50}
            List<List<int>> floors = GuanoSpreadRules.Floors(heights);
            Assert.That(floors.Count, Is.EqualTo(3));
            Assert.That(floors[0], Is.EquivalentTo(new[] { 1, 2, 4 }));
            Assert.That(floors[1], Is.EquivalentTo(new[] { 0, 3 }));
            Assert.That(floors[2], Is.EquivalentTo(new[] { 5 }));
        }

        [Test]
        public void Floors_AGapOfExactlyTheStepStaysOneFloor()
        {
            Assert.That(GuanoSpreadRules.Floors(new List<double> { 0, GuanoSpreadRules.FloorGap }).Count, Is.EqualTo(1));
            Assert.That(GuanoSpreadRules.Floors(new List<double> { 0, GuanoSpreadRules.FloorGap + 0.01 }).Count, Is.EqualTo(2));
        }

        [Test]
        public void FloorOrder_DealsOnePerFloorPerRound()
        {
            var heights = new List<double> { 10, 11, 12, 30, 31, 50 };
            int[] order = GuanoSpreadRules.FloorOrder(heights, Zero);
            Assert.That(order.OrderBy(i => i), Is.EqualTo(Enumerable.Range(0, 6)), "every spot once");
            var floorOf = new Func<int, int>(i => heights[i] < 20 ? 0 : heights[i] < 40 ? 1 : 2);
            Assert.That(order.Take(3).Select(floorOf), Is.EquivalentTo(new[] { 0, 1, 2 }), "the first round has one spot of each floor");
            Assert.That(order.Skip(3).Take(2).Select(floorOf), Is.EquivalentTo(new[] { 0, 1 }), "the second round: the floors that still have spots");
            Assert.That(floorOf(order[5]), Is.EqualTo(0));
        }

        [Test]
        public void FloorOrder_SixDepositsOnThreeFloors_AreTwoPerFloor()
        {
            var heights = new List<double>();
            for (int i = 0; i < 10; i++) { heights.Add(5 + i * 0.3); heights.Add(25 + i * 0.3); heights.Add(45 + i * 0.3); }
            int[] order = GuanoSpreadRules.FloorOrder(heights, new Random(9).NextDouble);
            var counts = order.Take(6).GroupBy(i => (int)(heights[i] / 20)).Select(g => g.Count());
            Assert.That(counts, Is.EqualTo(new[] { 2, 2, 2 }));
        }

        [Test]
        public void FloorOrder_NoSpots_IsEmpty()
        {
            Assert.That(GuanoSpreadRules.FloorOrder(new List<double>(), Zero), Is.Empty);
            Assert.That(GuanoSpreadRules.FloorOrder(null, Zero), Is.Empty);
            Assert.That(GuanoSpreadRules.Floors(null), Is.Empty);
        }

        [Test]
        public void FloorOrder_ShufflesWithinAFloor()
        {
            var heights = new List<double> { 1, 1.5, 2, 2.5 };
            int[] low = GuanoSpreadRules.FloorOrder(heights, () => 0d);
            int[] high = GuanoSpreadRules.FloorOrder(heights, () => 0.999d);
            Assert.That(low, Is.Not.EqualTo(high), "the draws decide the order inside a floor");
            Assert.That(low.OrderBy(i => i), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }
    }
}
