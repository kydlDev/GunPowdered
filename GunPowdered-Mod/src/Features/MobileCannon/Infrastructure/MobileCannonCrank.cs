using UnityEngine;
using GunPowdered.Core;
using KydlCore.Common;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>The look-at handle of the Mobile Cannon's crank (D-104): its hover text shows the elevation, and while the local player looks at it the mouse wheel turns it (MobileCannonScrollPatch).</summary>
    internal sealed class MobileCannonCrank : MonoBehaviour, Hoverable
    {
        internal const string ObjectName = "gp_crank_handle";

        internal MobileCannonAim Aim { get; set; }

        public string GetHoverName() => "Elevation crank";

        public float GetHoverOffset() => 0f;

        public string GetHoverText() => "Elevation crank: " + (Aim != null ? Aim.Pitch.ToString("0") : "?") + " deg\n[<color=yellow><b>Mouse wheel</b></color>] Raise / lower the barrel";

        /// <summary>The crank the local player looks at right now (null when none).</summary>
        internal static MobileCannonCrank Hovered()
        {
            if (RuleLog.Due("MobileCannonCrank.Hovered", 2d)) ModLog.Every("MobileCannonCrank.Hovered", 2d, $"MobileCannonCrank.Hovered()");
            Player p = Player.m_localPlayer;
            GameObject go = p != null ? p.GetHoverObject() : null;
            { var returnedAt26 = go != null ? go.GetComponent<MobileCannonCrank>() : null; if (RuleLog.Due("MobileCannonCrank.Hovered:26", 2d)) ModLog.Every("MobileCannonCrank.Hovered:26", 2d, $"MobileCannonCrank.Hovered: returns {RuleLog.Shown(returnedAt26)} (line 26)"); return returnedAt26; }
        }
    }
}
