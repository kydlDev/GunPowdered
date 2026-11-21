// kydl:read-only-observer - test-only: reads which models a dropped cannon ball draws (T-017); it drives no creature
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>T-017 lab hooks: a dropped cannon ball must be drawn once (its own model), not twice (plus the hand copy under "attach").</summary>
    internal static class BallModelTestHooks
    {
        /// <summary>
        /// The nearest world instance of <paramref name="prefab"/> to the local player (within 10 m): whether its own model and its hand copy are drawn.
        /// Returns "own model shown True/False, hand copy shown True/False, drawn N time(s)" or why not.
        /// </summary>
        [TestHook("GunPowdered.droppedBallModels")]
        static string DroppedBallModels(string prefab)
        {
            ModLog.Verbose("BallModelTestHooks.DroppedBallModels(prefab=" + prefab + ")");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("BallModelTestHooks.DroppedBallModels: no player - returns why"); return "no player"; }
            ItemDrop best = null;
            float bestDist = 10f;
            foreach (ItemDrop d in Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
            {
                if (d == null || Utils.GetPrefabName(d.gameObject) != prefab) continue;
                float dist = Vector3.Distance(d.transform.position, p.transform.position);
                if (dist < bestDist) { best = d; bestDist = dist; }
            }
            if (best == null) { ModLog.Verbose("BallModelTestHooks.DroppedBallModels: none within 10 m - returns why"); return "no " + prefab + " within 10 m"; }
            bool own = Shown(best.transform.Find("gp_model")), hand = Shown(best.transform.Find("attach/gp_model"));
            string s = "own model shown " + own + ", hand copy shown " + hand + ", drawn " + ((own ? 1 : 0) + (hand ? 1 : 0)) + " time(s)";
            ModLog.Info("BallModelTestHooks.DroppedBallModels: " + prefab + " " + bestDist.ToString("0.0") + " m away - returns " + s);
            return s;
        }

        static readonly HarmonyLib.AccessTools.FieldRef<VisEquipment, GameObject> RightItem = HarmonyLib.AccessTools.FieldRefAccess<VisEquipment, GameObject>("m_rightItemInstance");

        /// <summary>
        /// T-017: the ball the local player holds while loading is still drawn (the hand instance is the prefab's "attach" copy, which the ground fix must not
        /// hide). Returns "hand item NAME, ball shown True/False" or why not.
        /// </summary>
        [TestHook("GunPowdered.handBallShown")]
        static string HandBallShown()
        {
            ModLog.Verbose("BallModelTestHooks.HandBallShown()");
            VisEquipment vis = Player.m_localPlayer != null ? Player.m_localPlayer.GetComponent<VisEquipment>() : null;
            GameObject held = vis != null ? RightItem(vis) : null;
            if (held == null) { ModLog.Verbose("BallModelTestHooks.HandBallShown: nothing in the right hand - returns why"); return "nothing in the right hand"; }
            string s = "hand item " + held.name + ", ball shown " + Shown(held.transform.Find("gp_model"));
            ModLog.Info("BallModelTestHooks.HandBallShown: returns " + s);
            return s;
        }

        static bool Shown(Transform model)
        {
            if (model == null || !model.gameObject.activeInHierarchy) { if (RuleLog.IsOn()) ModLog.Every("BallModelTestHooks.Shown:none", 2d, "BallModelTestHooks.Shown: missing or inactive - returns false"); return false; }
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(false)) if (r.enabled) { if (RuleLog.IsOn()) ModLog.Every("BallModelTestHooks.Shown:yes", 2d, "BallModelTestHooks.Shown: returns true"); return true; }
            if (RuleLog.IsOn()) ModLog.Every("BallModelTestHooks.Shown:no", 2d, "BallModelTestHooks.Shown: returns false");
            return false;
        }
    }
}
