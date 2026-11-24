using GunPowdered.Core;
using GunPowdered.Integrations.Items;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-182 / D-183 test hooks: the flametal look and the punch after the last stick was set down. They call the real code paths.</summary>
    internal static class PlayTestFixHooks
    {
        /// <summary>
        /// Presses attack the way the game does (Humanoid.StartAttack) with whatever is in the hands and says whether the game started an attack:
        /// "attack started True/False, in attack True/False".
        /// </summary>
        [TestHook("GunPowdered.pressAttack")]
        static string PressAttack()
        {
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("PlayTestFixHooks.PressAttack: returns \"no player\" because p == null (line 19)"); return "no player"; }
            bool started = p.StartAttack(null, false);
            string s = "attack started " + started + ", in attack " + p.InAttack();
            ModLog.Info("PlayTestFixHooks: " + s);
            return s;
        }

        /// <summary>How many renderer slots of a spawned-in prefab's look use the game's FlametalNew material: "ingot slots N of M" or why not.</summary>
        [TestHook("GunPowdered.flametalSlots")]
        static string FlametalSlots(string prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            Material ingot = FlametalLook.Ingot;
            if (go == null || ingot == null) { ModLog.Verbose($"PlayTestFixHooks.FlametalSlots: returns why not because go == null || ingot == null ({prefab}: prefab {go != null}, ingot {ingot != null}) (line 32)"); return "prefab " + (go != null) + ", ingot " + (ingot != null); }
            int slots = 0, ingotSlots = 0, oldSlots = 0;
            var seen = new System.Text.StringBuilder();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                {
                    slots++;
                    if (m == ingot || (m != null && m.name.Replace(" (Instance)", "") == ingot.name && m.shader == ingot.shader)) ingotSlots++;     // the flight's clone holds a copy ("(Instance)") of the same material
                    else if (FlametalLook.IsFlametal(m)) oldSlots++;
                    seen.Append(r.name).Append('=').Append(m != null ? m.name : "<none>").Append(r.enabled ? "" : " (off)").Append("; ");
                }
            string s = $"ingot slots {ingotSlots} of {slots}, old flametal {oldSlots}";
            ModLog.Info($"PlayTestFixHooks: {prefab} {s} - {seen}");
            return s;
        }

        /// <summary>
        /// D-191: whether the prefab's inventory / build menu icon is the one rendered from its FlametalNew look: "rendered True|False|not yet, icon is the
        /// bundle picture True|False" - the icon the game shows now, compared with the bundle's own picture.
        /// </summary>
        [TestHook("GunPowdered.flametalIcon")]
        static string FlametalIcon(string prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            ItemDrop drop = go != null ? go.GetComponent<ItemDrop>() : null;
            Piece piece = go != null ? go.GetComponent<Piece>() : null;
            Sprite shown = drop != null && drop.m_itemData.m_shared.m_icons != null && drop.m_itemData.m_shared.m_icons.Length > 0 ? drop.m_itemData.m_shared.m_icons[0] : piece != null ? piece.m_icon : null;
            if (shown == null) { ModLog.Verbose($"PlayTestFixHooks.FlametalIcon: returns \"no icon\" because shown == null ({prefab}: prefab {go != null}) (line 59)"); return "no icon"; }
            string rendered = FlametalIcons.Rendered.TryGetValue(prefab, out bool ok) ? ok.ToString() : "not yet";
            bool bundle = shown == BundleVisuals.Icon(prefab);
            string s = $"rendered {rendered}, icon is the bundle picture {bundle}, {shown.texture.width}x{shown.texture.height}, {Framing(shown.texture)}";
            ModLog.Info($"PlayTestFixHooks: {prefab} {s}");
            return s;
        }

        /// <summary>"fills W% x H%, centre off X% Y%" of the square (FlametalIcons.Measure, the same measure the renderer sizes by), or "not measured".</summary>
        static string Framing(Texture2D t)
        {
            bool ok = FlametalIcons.Measure(t, out int fillW, out int fillH, out int offX, out int offY);
            string s = ok ? $"fills {fillW}% x {fillH}%, centre off {offX}% {offY}%" : "not measured";
            ModLog.Verbose($"PlayTestFixHooks.Framing: returns {s} for {(t != null ? t.name : "<none>")} (line 72)");
            return s;
        }
    }
}
