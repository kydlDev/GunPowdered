using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>
    /// T-017 (user 2026-11-21 02:51): a dropped cannon ball was drawn twice - its own model (root "gp_model") and the hand copy under "attach" that
    /// CannonSetup.AttachHandModel makes for holding it (the game shows only the "attach" child in a hand, taken from the PREFAB). A ball lying in the
    /// world is a whole instance of the prefab, so this hides the hand copy there; the prefab keeps it, so the ball still shows in the hand.
    /// </summary>
    internal sealed class CannonBallGroundModel : MonoBehaviour
    {
        void Awake()
        {
            ZNetView view = GetComponent<ZNetView>();
            if (view == null || view.GetZDO() == null) { ModLog.Verbose("CannonBallGroundModel.Awake: " + name + " is not a world instance (prefab or ghost) - the hand copy stays - returns"); return; }
            Transform hand = transform.Find("attach/gp_model");
            int hidden = 0;
            if (hand != null) foreach (Renderer r in hand.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; hidden++; }
            ModLog.Verbose("CannonBallGroundModel.Awake: " + name + " in the world - " + hidden + " hand-copy renderer(s) hidden, the ball shows once - returns");
        }
    }
}
