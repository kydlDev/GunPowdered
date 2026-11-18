using System;
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// Puts deposit prefabs into the world (Bat Guano heaps, Sulfurous Mud). Only the machine that creates a location or a dungeon calls this, so every
    /// deposit exists once; the deposit is a persistent network object like any vanilla pickable. The amount it gives is stored in its ZDO ("gp_amount")
    /// so whichever client owns it later hands out the same number.
    /// </summary>
    internal static class DepositSpawner
    {
        internal const string KeyAmount = "gp_amount";
        static readonly int FloorMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "terrain");

        /// <summary>Expected outcome: the new deposit, or null when the prefab is missing (the reason is logged).</summary>
        internal static GameObject Spawn(string prefabName, Vector3 position, int amount, int yawSeed)
        {
            try
            {
                GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
                if (prefab == null) { ModLog.Warn("DepositSpawner: prefab " + prefabName + " is not registered"); return null; }
                var yaw = Quaternion.Euler(0f, (yawSeed * 47) % 360, 0f);
                GameObject go = UnityEngine.Object.Instantiate(prefab, position, yaw);
                ZNetView view = go.GetComponent<ZNetView>();
                if (view != null && view.IsValid()) view.GetZDO().Set(KeyAmount, amount);
                ModLog.Decision("deposit placed", prefabName + " x" + amount, "at " + position.ToString("0"));
                return go;
            }
            catch (Exception e)
            {
                ModLog.Warn("DepositSpawner.Spawn: " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        /// <summary>The floor under a point (looks 3 m up and 6 m down). Expected outcome: true when a floor was found.</summary>
        internal static bool Floor(Vector3 around, out Vector3 floor)
        {
            RaycastHit hit;
            bool found = Physics.Raycast(around + Vector3.up * 3f, Vector3.down, out hit, 9f, FloorMask, QueryTriggerInteraction.Ignore);
            floor = found ? hit.point : around;
            if (found) return true;
            // no collider hit: right after a location's terrain changes are applied the ground collider is rebuilt and a ray finds nothing (lake ruin,
            // run extra2b), so the terrain height itself is used when it lies in the same window
            float ground = float.NaN;
            bool loaded = ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(around, out ground);
            bool inWindow = loaded && ground <= around.y + 3f && ground >= around.y - 6f;
            if (inWindow) floor = new Vector3(around.x, ground, around.z);
            KydlCore.Common.ModLog.Verbose("DepositSpawner: no collider under " + around.ToString("0.0") + ", terrain height "
                + (loaded ? ground.ToString("0.0") : "not loaded") + (inWindow ? " used" : " not used"));
            return inWindow;
        }

        /// <summary>A random point on the ring [minR, maxR] around the centre, from a seeded source of numbers.</summary>
        internal static Vector3 OnRing(Vector3 centre, float minR, float maxR, System.Random rng)
        {
            float angle = (float)(rng.NextDouble() * Math.PI * 2d);
            float r = minR + (float)rng.NextDouble() * (maxR - minR);
            return centre + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
        }

        /// <summary>Picks the given number of distinct entries (or all when there are fewer) from a list, using the seeded source.</summary>
        internal static List<T> Pick<T>(IList<T> source, int count, System.Random rng)
        {
            var pool = new List<T>(source);
            var picked = new List<T>();
            while (picked.Count < count && pool.Count > 0)
            {
                int i = rng.Next(pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return picked;
        }
    }
}
