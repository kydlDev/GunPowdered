using System.Linq;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// Test only: the ways a Mobile Cannon tow ends - the saddle taken off, the Asksvin killed, the tow joint broken - and the harness items they leave.
    /// Split from AsksvinTowTestHooks (class size, PROJECT_RULES 7); the Asksvin itself stays there.
    /// </summary>
    internal static class TowEndTestHooks
    {

        /// <summary>Takes the saddle off the lab's Asksvin with the game's own DropSaddle (as the owner's "remove saddle"). Returns "saddle dropped True/False".</summary>
        [TestHook("GunPowdered.asksvinDropSaddle")]
        static string DropSaddle()
        {
            Tameable t = AsksvinTowTestHooks.Animal != null ? AsksvinTowTestHooks.Animal.GetComponent<Tameable>() : null;
            if (t == null || Player.m_localPlayer == null) return "no asksvin";
            _lastAnimalPos = t.transform.position;
            var m = HarmonyLib.AccessTools.Method(typeof(Tameable), "DropSaddle");
            object r = m.GetParameters().Length == 1 ? m.Invoke(t, new object[] { Player.m_localPlayer.transform.position }) : m.Invoke(t, new object[0]);
            ModLog.Info("TowEndTestHooks: saddle dropped " + r);
            return "saddle dropped " + r;
        }

        /// <summary>Kills the lab's Asksvin (a hit with no attacker, so no tame protection is in the way). Returns "dead True".</summary>
        [TestHook("GunPowdered.asksvinKill")]
        static string Kill()
        {
            Character c = AsksvinTowTestHooks.Animal != null ? AsksvinTowTestHooks.Animal.GetComponent<Character>() : null;
            if (c == null) return "no asksvin";
            _lastAnimalPos = c.transform.position;
            var hit = new HitData { m_point = c.transform.position, m_dir = Vector3.down };
            hit.m_damage.m_blunt = c.GetHealth() * 10f + 10000f;
            c.Damage(hit);
            ModLog.Info("TowEndTestHooks: asksvin hit for " + hit.m_damage.m_blunt.ToString("0") + ", dead " + c.IsDead());
            return "killed";
        }

        /// <summary>Asksvin Harness items lying within 8 m of where the lab's Asksvin is or was. Returns "harness items on the ground N".</summary>
        [TestHook("GunPowdered.harnessItemsNear")]
        static string HarnessItemsNear()
        {
            Vector3 at = AsksvinTowTestHooks.Animal != null ? AsksvinTowTestHooks.Animal.transform.position : _lastAnimalPos;
            int n = 0;
            foreach (ItemDrop d in Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
                if (d != null && Utils.GetPrefabName(d.gameObject) == GunPowderedPrefabs.AsksvinHarness && Vector3.Distance(d.transform.position, at) < 8f) n += d.m_itemData.m_stack;
            ModLog.Info("TowEndTestHooks: harness items on the ground " + n);
            return "harness items on the ground " + n;
        }

        static Vector3 _lastAnimalPos;

        /// <summary>Sets the live tow joint's break force (N), so the next pull breaks it. Returns "break force N" or why not.</summary>
        [TestHook("GunPowdered.mobileCannonBreakForce")]
        static string BreakForce(double newtons)
        {
            GameObject cannon = MobileCannonTestHooks.Cannon;
            ConfigurableJoint j = cannon != null ? cannon.GetComponent<ConfigurableJoint>() : null;
            if (j == null) return "no tow joint";
            j.breakForce = (float)newtons;
            if (AsksvinTowTestHooks.Animal != null) _lastAnimalPos = AsksvinTowTestHooks.Animal.transform.position;
            return "break force " + newtons.ToString("0");
        }

    }
}
