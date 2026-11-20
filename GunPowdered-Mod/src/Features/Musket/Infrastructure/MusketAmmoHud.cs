using KydlCore.Common;
using TMPro;
using UnityEngine;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The musket's load next to the crosshair, "3/5" (user 2026-11-19 12:32, D-099): shown while the local player holds the musket, it follows every
    /// shot and every cartridge loaded. The text is a copy of the game's own hover-name text, so it has the HUD's font, size and colour; this class
    /// only places it right below the crosshair and sets the words.
    /// </summary>
    internal static class MusketAmmoHud
    {
        static readonly Vector2 Offset = new Vector2(45f, -30f);
        static TMP_Text _text;
        static int _shown = int.MinValue;

        /// <summary>What the counter shows now, or "hidden" (for test hooks).</summary>
        internal static string Current => _text != null && _text.gameObject.activeSelf ? _text.text : "hidden";

        /// <summary>Once per frame for the local player. Expected outcome: the counter shows the held musket's load, or is hidden without a musket.</summary>
        internal static void Refresh(Player local)
        {
            ItemDrop.ItemData musket = MusketGun.Held(local);
            if (_text == null)
            {
                if (musket == null || !Make()) { if (RuleLog.On) ModLog.Every("MusketAmmoHud.Refresh:29", 2d, "MusketAmmoHud.Refresh: returns (line 29)"); return; }
            }
            bool show = musket != null && !InventoryGui.IsVisible() && !Menu.IsVisible();
            if (_text.gameObject.activeSelf != show) _text.gameObject.SetActive(show);
            if (!show) { if (RuleLog.On) ModLog.Every("MusketAmmoHud.Refresh:33", 2d, "MusketAmmoHud.Refresh: returns (line 33)"); return; }
            int loaded = MusketGun.Loaded(musket);
            if (loaded == _shown) { if (RuleLog.On) ModLog.Every("MusketAmmoHud.Refresh:35", 2d, "MusketAmmoHud.Refresh: returns (line 35)"); return; }
            _text.text = MusketRules.LoadText(loaded);
            _shown = loaded;
            ModLog.Verbose("MusketAmmoHud: " + _text.text);
        }

        static bool Make()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_hoverName == null || hud.m_crosshair == null) { if (RuleLog.On) ModLog.Every("MusketAmmoHud.Make:44", 2d, "MusketAmmoHud.Make: returns false (line 44)"); return false; }
            GameObject go = Object.Instantiate(hud.m_hoverName.gameObject, hud.m_crosshair.transform.parent, false);
            go.name = "gp_musket_load";
            _text = go.GetComponent<TMP_Text>();
            if (_text == null)
            {
                Object.Destroy(go);
                { if (RuleLog.On) ModLog.Every("MusketAmmoHud.Make:51", 2d, "MusketAmmoHud.Make: returns false (line 51)"); return false; }
            }
            RectTransform cross = hud.m_crosshair.rectTransform, rt = (RectTransform)go.transform;
            rt.anchorMin = cross.anchorMin;
            rt.anchorMax = cross.anchorMax;
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(160f, 40f);
            rt.anchoredPosition = cross.anchoredPosition + Offset;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _shown = int.MinValue;
            ModLog.Verbose("MusketAmmoHud: counter made next to the crosshair");
            return true;
        }
    }
}
