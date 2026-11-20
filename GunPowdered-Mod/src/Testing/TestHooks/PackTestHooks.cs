using System.Collections.Generic;
using System.Linq;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Infrastructure;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the Deep North Dynamite Pack (D-108). Setting it down and lighting it use the dynamite hooks (the real code); these add the pack's
    /// numbers, targets set out beyond the pack (away from the player, who stands outside the 20 m blast), the on-screen countdown and the Fader key gate.
    /// </summary>
    internal static class PackTestHooks
    {
        sealed class Target { internal GameObject Go; internal string Name; internal float Metres; internal int PartsAtStart; }
        static readonly List<Target> Targets = new List<Target>();
        static readonly Dictionary<string, bool> KeysAtStart = new Dictionary<string, bool>();

        /// <summary>"pack P, radius R, pickaxe X, blunt B chop C tier T, props P chars C, creatures B + fire F, crater W x D".</summary>
        [TestHook("GunPowdered.packInfo")]
        static string Info()
        {
            DynamiteSetup.ChargeKind k = DynamiteSetup.PackKind;
            if (k.Explosion == null) return "no pack blast";
            Aoe props = k.Explosion.GetComponent<Aoe>();
            Transform child = k.Explosion.transform.Find("gp_creature_blast");
            Aoe creatures = child != null ? child.GetComponent<Aoe>() : null;
            TerrainOp dig = k.Crater != null ? k.Crater.GetComponent<TerrainOp>() : null;
            return "pack " + (k.Charge != null) + ", radius " + props.m_radius.ToString("0.#") + ", pickaxe " + props.m_damage.m_pickaxe.ToString("0") + ", blunt " + props.m_damage.m_blunt.ToString("0")
                + " chop " + props.m_damage.m_chop.ToString("0") + " tier " + props.m_toolTier + ", props " + props.m_hitProps + " chars " + props.m_hitCharacters
                + ", creatures " + (creatures != null ? creatures.m_damage.m_blunt.ToString("0") + " + fire " + creatures.m_damage.m_fire.ToString("0") + " radius " + creatures.m_radius.ToString("0.#") : "none")
                + ", crater " + (dig != null ? (dig.m_settings.m_raiseRadius * 2f).ToString("0.0") + " x " + (-dig.m_settings.m_raiseDelta).ToString("0.0") : "none");
        }

        /// <summary>
        /// Puts <paramref name="prefab"/> on the ground <paramref name="metres"/> beyond the nearest charge, on the line from the player through the charge
        /// (so it is that far from the blast and farther from the player). Returns "spawned P D m beyond the charge (N parts)".
        /// </summary>
        [TestHook("GunPowdered.packSpawnBeyond")]
        static string SpawnBeyond(string prefab, double metres)
        {
            Player p = Player.m_localPlayer;
            DynamiteCharge c = NearestCharge();
            GameObject pf = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (p == null || c == null) return "no player or no charge";
            if (pf == null) return "no prefab " + prefab;
            Vector3 dir = Vector3.ProjectOnPlane(c.transform.position - p.transform.position, Vector3.up).normalized;
            Vector3 at = c.transform.position + dir * (float)metres;
            float h;
            if (ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h;
            GameObject go = Object.Instantiate(pf, at, Quaternion.LookRotation(-dir));
            var t = new Target { Go = go, Name = prefab, Metres = (float)metres, PartsAtStart = Parts(go) };
            Targets.Add(t);
            ModLog.Info("PackTestHooks: " + prefab + " " + metres.ToString("0") + " m beyond the charge at " + at.ToString("0.0") + ", parts " + t.PartsAtStart);
            return "spawned " + prefab + " " + metres.ToString("0") + " m beyond the charge (" + t.PartsAtStart + " parts)";
        }

        /// <summary>Each target the test set out: "name@D gone" or "name@D standing (left/at start parts)", joined with "; ".</summary>
        [TestHook("GunPowdered.packTargets")]
        static string TargetState()
        {
            string s = string.Join("; ", Targets.Select(t => t.Name + "@" + t.Metres.ToString("0") + " " + (t.Go == null ? "gone" : "standing (" + Parts(t.Go) + "/" + t.PartsAtStart + " parts)")).ToArray());
            ModLog.Info("PackTestHooks: targets - " + s);
            return s;
        }

        /// <summary>Removes what packSpawnBeyond set out (what the blast left). Returns "removed N".</summary>
        [TestHook("GunPowdered.packCleanup")]
        static string Cleanup()
        {
            int n = 0;
            foreach (Target t in Targets)
            {
                if (t.Go == null) continue;
                ZNetView v = t.Go.GetComponent<ZNetView>();
                if (v != null && v.IsValid()) { v.ClaimOwnership(); ZNetScene.instance.Destroy(t.Go); } else Object.Destroy(t.Go);
                n++;
            }
            Targets.Clear();
            return "removed " + n;
        }

        /// <summary>The countdown the local player saw: "shows N, last 'text'".</summary>
        [TestHook("GunPowdered.packTimer")]
        static string Timer() => "shows " + DynamiteCharge.TimerShows + ", last '" + (DynamiteCharge.LastTimerText ?? "") + "'";

        /// <summary>Sets or clears a world key; the first call per key remembers how it was (globalKeysRestore). Returns "key K was X, now Y".</summary>
        [TestHook("GunPowdered.globalKey")]
        static string GlobalKey(string key, bool on)
        {
            ZoneSystem zs = ZoneSystem.instance;
            if (zs == null) return "no zone system";
            bool was = zs.GetGlobalKey(key);
            if (!KeysAtStart.ContainsKey(key)) KeysAtStart[key] = was;
            if (on) zs.SetGlobalKey(key); else zs.RemoveGlobalKey(key);
            return "key " + key + " was " + was + ", now " + zs.GetGlobalKey(key);
        }

        /// <summary>Puts every key globalKey touched back the way it was. Returns "restored N".</summary>
        [TestHook("GunPowdered.globalKeysRestore")]
        static string GlobalKeysRestore()
        {
            ZoneSystem zs = ZoneSystem.instance;
            int n = 0;
            if (zs != null)
                foreach (var kv in KeysAtStart) { if (kv.Value) zs.SetGlobalKey(kv.Key); else zs.RemoveGlobalKey(kv.Key); n++; }
            KeysAtStart.Clear();
            return "restored " + n;
        }

        /// <summary>
        /// The pack's recipe as the game sees it right after the player asks for the craftable list (that call runs RecipeGatePatch):
        /// "recipe on X, station S level L, needs A n, B n, ...".
        /// </summary>
        [TestHook("GunPowdered.packRecipe")]
        static string Recipe()
        {
            Player p = Player.m_localPlayer;
            if (p == null || ObjectDB.instance == null) return "no player";
            var list = new List<global::Recipe>();
            p.GetAvailableRecipes(ref list);
            global::Recipe r = ObjectDB.instance.m_recipes.FirstOrDefault(x => x != null && x.m_item != null && x.m_item.name == GunPowderedPrefabs.DeepNorthPack);
            if (r == null) return "no recipe";
            return "recipe on " + r.m_enabled + ", station " + (r.m_craftingStation != null ? r.m_craftingStation.name : "none") + " level " + r.m_minStationLevel
                + ", needs " + string.Join(", ", r.m_resources.Select(x => (x.m_resItem != null ? x.m_resItem.name : "?") + " " + x.m_amount).ToArray());
        }

        /// <summary>
        /// Uses up the game's backstab on the nearest creature of this prefab: an explosion hitting a creature that is not alerted is multiplied by
        /// Aoe.m_backstabBonus (4, vanilla bombs too), once per creature per 300 s (Character.m_backstabTime). Trolls are never "alerted"
        /// (BaseAI.Alert does nothing for them, lab a10pack3), so the hook sets that time to now. A test that measures the blast's own creature damage
        /// calls this first (D-125). Returns "backstab used on P" or "no P".
        /// </summary>
        [TestHook("GunPowdered.creatureBackstabUsed")]
        static string BackstabUsed(string prefab)
        {
            Player p = Player.m_localPlayer;
            Character best = null;
            float bestDist = 60f;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c == p || c.IsDead() || Utils.GetPrefabName(c.gameObject) != prefab) continue;
                float d = p != null ? Vector3.Distance(c.transform.position, p.transform.position) : 0f;
                if (d < bestDist) { best = c; bestDist = d; }
            }
            if (best == null) return "no " + prefab;
            HarmonyLib.AccessTools.Field(typeof(Character), "m_backstabTime").SetValue(best, Time.time);
            ModLog.Info("PackTestHooks: backstab of " + best.name + " used up (alerted " + (best.GetComponent<BaseAI>() != null && best.GetComponent<BaseAI>().IsAlerted()) + ")");
            return "backstab used on " + prefab;
        }

        static DynamiteCharge NearestCharge()
        {
            Player p = Player.m_localPlayer;
            DynamiteCharge best = null;
            float d = float.MaxValue;
            foreach (DynamiteCharge c in Object.FindObjectsByType<DynamiteCharge>(FindObjectsSortMode.None))
            {
                float x = p != null ? Vector3.Distance(c.transform.position, p.transform.position) : 0f;
                if (x < d) { d = x; best = c; }
            }
            return best;
        }

        // parts still standing: a MineRock5's hit areas with health left, else 1 for a whole object
        static int Parts(GameObject go)
        {
            if (go == null) return 0;
            MineRock5 rock = go.GetComponent<MineRock5>();
            if (rock == null) return 1;
            var areas = HarmonyLib.AccessTools.Field(typeof(MineRock5), "m_hitAreas")?.GetValue(rock) as System.Collections.IList;
            if (areas == null) return -1;
            int n = 0;
            foreach (object a in areas)
            {
                object h = a != null ? HarmonyLib.AccessTools.Field(a.GetType(), "m_health")?.GetValue(a) : null;
                if (h is float f && f > 0f) n++;
            }
            return n;
        }
    }
}
