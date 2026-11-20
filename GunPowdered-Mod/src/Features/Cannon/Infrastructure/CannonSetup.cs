using Jotunn.Entities;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.Items;
using GunPowdered.Core;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>
    /// Wires the Hand Cannon's prefabs (D-052): the Cannon Stock gets the mounted barrel from the bundle (hidden until a cannon is mounted) on a
    /// turning pivot together with the stock's look, and its behaviour (CannonMount); the Cannon Ball item gets a hand model (for the loading
    /// animation) and the flying ball is made: a vanilla projectile clone (network view + synced transform) whose own flight is replaced by
    /// CannonBallFlight and whose look is the bundle's cast ball.
    /// </summary>
    internal static class CannonSetup
    {
        internal const string ProjectileId = "GP_CannonBall_projectile";
        const string StockBundleId = "GP_CannonStock", BarrelChild = "mounted_barrel";
        static readonly string[] ProjectileBases = { "oozebomb_projectile", "bilebomb_projectile" };
        static GameObject _projectile;

        /// <summary>The flying ball prefab (null before the ball item was configured or when that failed).</summary>
        internal static GameObject Projectile => _projectile;

        /// <summary>
        /// The stock: the look and the barrel turn on one pivot, the solid colliders stay where the piece stands. Expected outcome: true when the
        /// stock has a pivot with the barrel and the CannonMount behaviour.
        /// </summary>
        internal static bool ConfigureStock(GameObject stock)
        {
            Transform pivot = stock.transform.Find(CannonMount.PivotName);
            if (pivot == null)
            {
                pivot = new GameObject(CannonMount.PivotName).transform;
                pivot.SetParent(stock.transform, false);
                pivot.gameObject.layer = stock.layer;
            }
            Transform look = stock.transform.Find("gp_model");
            if (look != null) look.SetParent(pivot, true);
            GameObject barrel = BundleVisuals.CopyChild(StockBundleId, stock, BarrelChild, CannonMount.BarrelName);
            if (barrel != null)
            {
                barrel.transform.SetParent(pivot, true);
                foreach (Collider c in barrel.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);   // the ball must not hit its own barrel
                barrel.SetActive(false);
            }
            if (stock.GetComponent<CannonMount>() == null) stock.AddComponent<CannonMount>();
            bool ok = barrel != null && stock.GetComponent<CannonMount>() != null;
            ModLog.Verbose("CannonSetup: stock configured - look on pivot " + (look != null) + ", barrel " + (barrel != null) + ", ok " + ok);
            return ok;
        }

        /// <summary>
        /// The ball item: a copy of its model under "attach" (the game shows only that child in a hand) and the flying ball prefab. Expected outcome:
        /// true when both exist.
        /// </summary>
        internal static bool ConfigureBall(GameObject ballItem)
        {
            bool hand = AttachHandModel(ballItem);
            if (_projectile == null) _projectile = MakeProjectile();
            ModLog.Verbose("CannonSetup: ball configured - hand model " + hand + ", flight prefab " + (_projectile != null));
            return hand && _projectile != null;
        }

        static bool AttachHandModel(GameObject item)
        {
            Transform model = item.transform.Find("gp_model");
            if (model == null) { ModLog.Warn("CannonSetup: the ball item has no bundle model, no hand model"); return false; }
            Transform attach = item.transform.Find("attach");
            if (attach == null)
            {
                attach = new GameObject("attach").transform;
                attach.SetParent(item.transform, false);
                attach.gameObject.layer = item.layer;
                CopyGrip(attach);
            }
            if (attach.Find("gp_model") != null) { if (RuleLog.IsOn()) ModLog.Verbose("CannonSetup.AttachHandModel: returns true (line 78)"); return true; }
            GameObject copy = Object.Instantiate(model.gameObject, attach);
            copy.name = "gp_model";
            // the ball sits where the vanilla Ooze Bomb's ball sits in the hand (centred on the hand joint it was hidden inside the fist, run cannonV5);
            // the model's origin is its bottom
            copy.transform.localPosition = HeldBallCentre() - (Vector3.up * HeldBallRadius);
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            { if (RuleLog.IsOn()) ModLog.Verbose("CannonSetup.AttachHandModel: returns true (line 88)"); return true; }
        }

        // our "attach" takes the Ooze Bomb attach's own place under its item, so the ball centre measured in that space means the same in ours
        static void CopyGrip(Transform attach)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonSetup.CopyGrip(attach={RuleLog.Shown(attach)})");
            GameObject bomb = PrefabManager.Instance.GetPrefab("BombOoze");
            Transform grip = bomb != null ? bomb.transform.Find("attach") : null;
            if (grip == null) { if (RuleLog.IsOn()) ModLog.Verbose("CannonSetup.CopyGrip: returns (line 97)"); return; }
            attach.localPosition = grip.localPosition;
            attach.localRotation = grip.localRotation;
            attach.localScale = grip.localScale;
        }

        const float HeldBallRadius = 0.05f;
        static readonly Vector3 FallbackCentre = new Vector3(0f, 0.08f, 0.04f);

        // where a held ball's centre is, in the "attach" space of a hand item: the vanilla Ooze Bomb's ball (an item made to be held as a ball)
        static Vector3 HeldBallCentre()
        {
            GameObject bomb = PrefabManager.Instance.GetPrefab("BombOoze");
            Transform attach = bomb != null ? bomb.transform.Find("attach") : null;
            Renderer r = attach != null ? attach.GetComponentInChildren<Renderer>(true) : null;
            if (r == null)
            {
                ModLog.Warn("CannonSetup: no Ooze Bomb hand model to copy the grip from, the ball sits at " + FallbackCentre);
                return FallbackCentre;
            }
            Vector3 centre = attach.InverseTransformPoint(r.transform.position);
            ModLog.Verbose("CannonSetup: the held ball centre is the Ooze Bomb's, " + centre.ToString("0.000"));
            return centre;
        }

        static GameObject MakeProjectile()
        {
            foreach (string b in ProjectileBases)
            {
                if (PrefabManager.Instance.GetPrefab(b) == null) { ModLog.Warn("CannonSetup: vanilla base " + b + " not found, trying the next one"); continue; }
                GameObject clone = PrefabManager.Instance.CreateClonedPrefab(ProjectileId, b);
                if (clone == null) continue;
                foreach (global::Projectile p in clone.GetComponentsInChildren<global::Projectile>(true)) Object.DestroyImmediate(p);
                foreach (Light l in clone.GetComponentsInChildren<Light>(true)) l.enabled = false;
                foreach (AudioSource a in clone.GetComponentsInChildren<AudioSource>(true)) a.enabled = false;
                foreach (Rigidbody r in clone.GetComponentsInChildren<Rigidbody>(true)) { r.isKinematic = true; r.useGravity = false; }
                BundleVisuals.Apply(ProjectileId, clone);
                foreach (Collider c in clone.GetComponentsInChildren<Collider>(true)) c.enabled = false;           // the flight tests the world itself
                if (clone.GetComponent<ZSyncTransform>() == null) clone.AddComponent<ZSyncTransform>();
                clone.AddComponent<CannonBallFlight>();
                PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
                ModLog.Verbose("CannonSetup: " + ProjectileId + " cloned from " + b);
                return clone;
            }
            ModLog.Error("CannonSetup: no vanilla base for " + ProjectileId);
            return null;
        }
    }
}
