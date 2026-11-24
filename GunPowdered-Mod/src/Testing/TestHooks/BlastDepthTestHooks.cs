using System.Reflection;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Infrastructure;
using GunPowdered.Features.Grenades.Infrastructure;
using GunPowdered.Features.Musket.Infrastructure;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// D-182 (user play test 2026-11-24) test hooks: blasts above the ground (the pack on a rock dug nothing), the grenade's hole and pickaxe, the musket
    /// facing the aim and reloading by itself. They call the real code (ChargeKind.Blast, the landed grenade's own fuse, Player.AlwaysRotateCamera).
    /// </summary>
    internal static class BlastDepthTestHooks
    {
        /// <summary>
        /// Sets off a "stick" or "pack" blast <paramref name="up"/> m above the ground <paramref name="dz"/> m in front of the player (like a charge lying on
        /// a rock) through the real ChargeKind.Blast, after marking the ground there. Returns "blast N object(s), U m above ground G" or why not.
        /// </summary>
        [TestHook("GunPowdered.blastAbove")]
        static string BlastAbove(string kind, double up, double dz)
        {
            Player p = Player.m_localPlayer;
            DynamiteSetup.ChargeKind k = kind == "pack" ? DynamiteSetup.PackKind : DynamiteSetup.StickKind;
            if (p == null || k.Explosion == null) { ModLog.Verbose("BlastDepthTestHooks.BlastAbove: returns \"no player or no blast\" because p == null || k.Explosion == null (line 27)"); return "no player or no blast"; }
            Vector3 at = p.transform.position + p.transform.forward * (float)dz;
            string mark = DynamiteTestHooks.MarkAt(at);
            float ground = DynamiteTestHooks.Ground(at);
            if (float.IsNaN(ground)) { ModLog.Verbose("BlastDepthTestHooks.BlastAbove: returns the mark because the ground is not loaded (line 31)"); return mark; }
            at.y = ground + (float)up;
            int made = DynamiteSetup.Blast(k, at, p);
            string s = $"blast {made} object(s), {up:0.0} m above ground {ground:0.00}";
            ModLog.Info("BlastDepthTestHooks: " + k.Name + " " + s);
            return s;
        }

        /// <summary>Puts a landed Spiked Grenade (its fuse already burning) on the ground <paramref name="dz"/> m in front of the player and marks the ground there. Returns "grenade landed, marked G".</summary>
        [TestHook("GunPowdered.grenadeLand")]
        static string GrenadeLand(double dz)
        {
            Player p = Player.m_localPlayer;
            if (p == null || GrenadeSetup.Landed == null) { ModLog.Verbose("BlastDepthTestHooks.GrenadeLand: returns \"no player or no landed grenade\" because p == null || GrenadeSetup.Landed == null (line 46)"); return "no player or no landed grenade"; }
            Vector3 at = p.transform.position + p.transform.forward * (float)dz;
            string mark = DynamiteTestHooks.MarkAt(at);
            at.y = DynamiteTestHooks.Ground(at) + 0.05f;
            Object.Instantiate(GrenadeSetup.Landed, at, Quaternion.identity);
            string s = "grenade landed, " + mark;
            ModLog.Info("BlastDepthTestHooks: " + s);
            return s;
        }

        /// <summary>How many landed grenades burst on this client so far.</summary>
        [TestHook("GunPowdered.grenadeBursts")]
        static int GrenadeBursts() => GrenadeFuse.Bursts;

        /// <summary>The grenade blast's rock numbers and its hole: "pickaxe P tier T, crater W x D".</summary>
        [TestHook("GunPowdered.grenadeBlastStats")]
        static string GrenadeBlastStats()
        {
            Aoe blast = GrenadeSetup.Explosion != null ? GrenadeSetup.Explosion.GetComponent<Aoe>() : null;
            TerrainOp dig = GrenadeSetup.Crater != null ? GrenadeSetup.Crater.GetComponent<TerrainOp>() : null;
            if (blast == null || dig == null) { ModLog.Verbose("BlastDepthTestHooks.GrenadeBlastStats: returns \"no blast or no crater\" because blast == null || dig == null (line 67)"); return "no blast or no crater"; }
            string s = $"pickaxe {blast.m_damage.m_pickaxe:0} tier {blast.m_toolTier}, crater {dig.m_settings.m_raiseRadius * 2f:0.0} x {-dig.m_settings.m_raiseDelta:0.0}";
            ModLog.Info("BlastDepthTestHooks: grenade " + s);
            return s;
        }

        static readonly MethodInfo AlwaysRotate = typeof(Player).GetMethod("AlwaysRotateCamera", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        /// <summary>D-182: whether the local player turns with the camera right now (the game's own question, MusketFacePatch answers it for the musket). "faces the aim True/False".</summary>
        [TestHook("GunPowdered.facesAim")]
        static string FacesAim()
        {
            Player p = Player.m_localPlayer;
            if (p == null || AlwaysRotate == null) { ModLog.Verbose("BlastDepthTestHooks.FacesAim: returns \"no player\" because p == null || AlwaysRotate == null (line 80)"); return "no player"; }
            string s = "faces the aim " + (bool)AlwaysRotate.Invoke(p, null);
            ModLog.Info("BlastDepthTestHooks: " + s);
            return s;
        }

        /// <summary>D-182: what the last automatic reload after a shot did ("starts in N s", "began: ...", "no cartridges", "N still loaded").</summary>
        [TestHook("GunPowdered.musketAutoReload")]
        static string MusketAutoReload() => MusketReloader.LastAuto;

        /// <summary>The crater depths now configured: "stick D, pack P, grenade G".</summary>
        [TestHook("GunPowdered.craterDepths")]
        static string CraterDepths() => $"stick {ModSettings.DynamiteCraterDepth.Value:0.0}, pack {ModSettings.PackCraterDepth.Value:0.0}, grenade {ModSettings.GrenadeCraterDepth.Value:0.0}";
    }
}
