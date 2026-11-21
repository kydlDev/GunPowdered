using UnityEngine;
using GunPowdered.Core;
using KydlCore.Common;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>A part of the Mobile Cannon the player looks at and uses (muzzle: load, touch hole: light the fuse). The cart itself keeps its own E (grab).</summary>
    internal abstract class MobileCannonPart : MonoBehaviour, Hoverable, Interactable
    {
        internal MobileCannonGun Gun { get; set; }

        public abstract string GetHoverName();

        public abstract string GetHoverText();

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonPart.Interact", 2d, $"MobileCannonPart.Interact(user={RuleLog.Shown(user)}, hold={RuleLog.Shown(hold)}, alt={RuleLog.Shown(alt)})");
            Player p = user as Player;
            if (hold || p == null || Gun == null) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonPart.Interact:22", 2d, "MobileCannonPart.Interact: returns false (line 22)"); return false; }
            { var returnedAt23 = Use(p); if (RuleLog.IsOn()) ModLog.Every("MobileCannonPart.Interact:23", 2d, $"MobileCannonPart.Interact: returns {RuleLog.Shown(returnedAt23)} (line 23)"); return returnedAt23; }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        protected abstract bool Use(Player p);

        protected string LoadState() => Gun == null ? "?" : Gun.Loading ? "loading..." : Gun.Lit ? "fuse burning!" : Gun.Loaded ? "loaded" : "empty";
    }

    /// <summary>The muzzle: E loads gunpowder and one Cannon Ball.</summary>
    internal sealed class MobileCannonMuzzle : MobileCannonPart
    {
        public override string GetHoverName() => "Mobile Cannon muzzle";

        public override string GetHoverText() => Localization.instance.Localize("Mobile Cannon (" + LoadState() + ")\n[<color=yellow><b>$KEY_Use</b></color>] Load gunpowder and a Flametal Cannon Ball");

        protected override bool Use(Player p) => Gun.Load(p);
    }

    /// <summary>The touch hole: E with a torch in hand lights the fuse.</summary>
    internal sealed class MobileCannonTouchHole : MobileCannonPart
    {
        public override string GetHoverName() => "Mobile Cannon touch hole";

        public override string GetHoverText() => Localization.instance.Localize("Mobile Cannon (" + LoadState() + ")\n[<color=yellow><b>$KEY_Use</b></color>] Light the fuse (torch in hand)");

        protected override bool Use(Player p) => Gun.Light(p) == null;
    }
}
