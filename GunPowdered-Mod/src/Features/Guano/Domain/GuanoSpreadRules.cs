using System;
using System.Collections.Generic;
using GunPowdered.Core;

namespace GunPowdered.Features.Guano.Domain
{
    /// <summary>
    /// D-151 (user 2026-11-21 12:25): the deposits of a cave are "distributed evenly per floors of a cave". The cave's candidate spots are grouped into
    /// floors by their height (a new floor starts where the next spot is more than <see cref="FloorGap"/> m above the one below), each floor's spots are
    /// shuffled, and the spots are dealt round-robin from the lowest floor up: one per floor, then a second per floor, and so on. The placer takes them in
    /// this order (skipping a spot without ground), so N deposits land on the floors as evenly as N allows. Pure: the caller hands in [0, 1) numbers.
    /// </summary>
    internal static class GuanoSpreadRules
    {
        /// <summary>Height step (m) that starts a new floor: Valheim's cave levels lie about 8-12 m apart, a room's own floor varies by a few metres.</summary>
        internal const double FloorGap = 5.0;

        /// <summary>
        /// The order to try the spots in. Expected outcome: every index of <paramref name="heights"/> exactly once; the first k indices hold spots of
        /// min(k, floors) different floors; an empty input gives an empty order.
        /// </summary>
        internal static int[] FloorOrder(IList<double> heights, Func<double> next01)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoSpreadRules.FloorOrder(heights={RuleLog.Shown(heights == null ? 0 : heights.Count)} spot(s))");
            if (heights == null || heights.Count == 0) { if (RuleLog.IsOn()) RuleLog.Verbose("GuanoSpreadRules.FloorOrder: no spots - returns an empty order"); return new int[0]; }
            List<List<int>> floors = Floors(heights);
            foreach (List<int> floor in floors) Shuffle(floor, next01);
            var order = new List<int>(heights.Count);
            for (int round = 0; order.Count < heights.Count; round++)
                foreach (List<int> floor in floors)
                    if (round < floor.Count) order.Add(floor[round]);
            if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoSpreadRules.FloorOrder: {floors.Count} floor(s) - returns {order.Count} index(es)");
            return order.ToArray();
        }

        /// <summary>The spots grouped into floors, lowest first. Expected outcome: every index in exactly one floor; floors ordered by height.</summary>
        internal static List<List<int>> Floors(IList<double> heights)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoSpreadRules.Floors({RuleLog.Shown(heights == null ? 0 : heights.Count)} spot(s))");
            var floors = new List<List<int>>();
            if (heights == null || heights.Count == 0) { if (RuleLog.IsOn()) RuleLog.Verbose("GuanoSpreadRules.Floors: no spots - returns none"); return floors; }
            var byHeight = new List<int>(heights.Count);
            for (int i = 0; i < heights.Count; i++) byHeight.Add(i);
            byHeight.Sort((a, b) => { int byY = heights[a].CompareTo(heights[b]); return byY != 0 ? byY : a.CompareTo(b); });   // Sonar S1244: no exact float compare
            double previous = double.NegativeInfinity;
            foreach (int i in byHeight)
            {
                if (floors.Count == 0 || heights[i] - previous > FloorGap) floors.Add(new List<int>());
                floors[floors.Count - 1].Add(i);
                previous = heights[i];
            }
            if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoSpreadRules.Floors: returns {floors.Count} floor(s)");
            return floors;
        }

        static void Shuffle(List<int> list, Func<double> next01)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoSpreadRules.Shuffle({list.Count} item(s))");
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Roll.IntInclusive(0, i, next01());
                int t = list[i]; list[i] = list[j]; list[j] = t;
            }
            if (RuleLog.IsOn()) RuleLog.Verbose("GuanoSpreadRules.Shuffle: returns");
        }
    }
}
