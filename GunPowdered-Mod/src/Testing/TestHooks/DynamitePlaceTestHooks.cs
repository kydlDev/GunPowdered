// kydl:read-only-observer - test-only: puts a wall in front of the player and reads where a dynamite stick was set down (D-148); it drives no creature
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-148 lab hooks: something in the way of the stick, and where the stick ended up relative to the player.</summary>
    internal static class DynamitePlaceTestHooks
    {
        static GameObject _wall;

        /// <summary>Builds <paramref name="prefab"/> <paramref name="metres"/> in front of the local player, across the way, on the ground. Returns "wall at ..." or why not.</summary>
        [TestHook("GunPowdered.wallAhead")]
        static string WallAhead(string prefab, double metres)
        {
            ModLog.Verbose($"DynamitePlaceTestHooks.WallAhead(prefab={prefab}, metres={metres})");
            Player p = Player.m_localPlayer;
            GameObject piece = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (p == null || piece == null) { ModLog.Verbose("DynamitePlaceTestHooks.WallAhead: no player or no " + prefab + " - returns why"); return "no player or no " + prefab; }
            Vector3 fwd = Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized;
            Vector3 at = p.transform.position + fwd * (float)metres;
            _wall = Object.Instantiate(piece, at, Quaternion.LookRotation(fwd));
            Collider[] cs = _wall.GetComponentsInChildren<Collider>();
            float bottom = float.MaxValue;
            foreach (Collider c in cs) bottom = Mathf.Min(bottom, c.bounds.min.y);
            if (cs.Length > 0) _wall.transform.position += Vector3.up * (p.transform.position.y - bottom);     // its foot on the ground (a wall's pivot is in its middle)
            string s = "wall at " + _wall.transform.position.ToString("0.00") + " (" + prefab + ")";
            ModLog.Verbose("DynamitePlaceTestHooks.WallAhead: returns " + s);
            return s;
        }

        /// <summary>Takes the wall away again. Returns "removed" or "no wall".</summary>
        [TestHook("GunPowdered.wallAheadRemove")]
        static string Remove()
        {
            ModLog.Verbose("DynamitePlaceTestHooks.Remove()");
            if (_wall == null) { ModLog.Verbose("DynamitePlaceTestHooks.Remove: no wall - returns"); return "no wall"; }
            ZNetView v = _wall.GetComponent<ZNetView>();
            if (v != null && v.IsValid()) v.Destroy(); else Object.Destroy(_wall);
            _wall = null;
            ModLog.Verbose("DynamitePlaceTestHooks.Remove: returns removed");
            return "removed";
        }

        /// <summary>
        /// Where the stick closest to the player stands, from the player: "ahead A m, up U m, wall W m ahead" (wall: the distance of the wall hook's wall, or
        /// "none"). Returns why not when no stick stands near.
        /// </summary>
        [TestHook("GunPowdered.stickFromPlayer")]
        static string StickFromPlayer()
        {
            ModLog.Verbose("DynamitePlaceTestHooks.StickFromPlayer()");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("DynamitePlaceTestHooks.StickFromPlayer: no player - returns why"); return "no player"; }
            Transform best = null;
            float bestDist = 10f;
            foreach (GunPowdered.Features.Dynamite.Infrastructure.DynamiteCharge c in Object.FindObjectsByType<GunPowdered.Features.Dynamite.Infrastructure.DynamiteCharge>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(c.transform.position, p.transform.position);
                if (d < bestDist) { best = c.transform; bestDist = d; }
            }
            if (best == null) { ModLog.Verbose("DynamitePlaceTestHooks.StickFromPlayer: none within 10 m - returns why"); return "no stick within 10 m"; }
            Vector3 fwd = Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized;
            Vector3 rel = best.position - p.transform.position;
            string wall = _wall != null ? Vector3.Dot(_wall.transform.position - p.transform.position, fwd).ToString("0.00") + " m ahead" : "none";
            string s = $"ahead {Vector3.Dot(rel, fwd):0.00} m, up {rel.y:0.00} m, wall {wall}";
            ModLog.Info("DynamitePlaceTestHooks.StickFromPlayer: returns " + s);
            return s;
        }
    }
}
