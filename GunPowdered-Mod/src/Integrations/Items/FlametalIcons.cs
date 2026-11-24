using System.Collections.Generic;
using GunPowdered.Core;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Integrations.Items
{
    /// <summary>
    /// D-191 (user 2026-11-24 17:49): the inventory / build menu icon of every prefab FlametalLook re-textured (the Flametal Cannon Ball, the Mobile
    /// Cannon, the Flametal Heater, the drying oven) is rendered again from its new look with Jotunn's icon renderer, so KY-Items, the inventory and the
    /// build menu show the FlametalNew look. Once per game start, <see cref="FlametalIconRules.DelaySeconds"/> after the player is in a world (rendering
    /// at load gave white icons in DrainTarPit). A prefab whose render fails keeps the bundle's picture.
    /// </summary>
    internal static class FlametalIcons
    {
        const int Size = 128;
        static float _inWorldSince = -1f;
        static bool _done;

        /// <summary>Per prefab id: whether its icon is the rendered one now (false: the render failed, the bundle picture stays).</summary>
        internal static readonly Dictionary<string, bool> Rendered = new Dictionary<string, bool>();

        /// <summary>Called every frame by the plugin; renders the icons once when <see cref="FlametalIconRules.Due"/> says so.</summary>
        internal static void Tick()
        {
            bool inWorld = Player.m_localPlayer != null;
            float since = inWorld && _inWorldSince >= 0f ? Time.realtimeSinceStartup - _inWorldSince : 0f;
            ModLog.Every("FlametalIcons.Tick", 30d, $"FlametalIcons.Tick: in world {inWorld} for {since:0.0} s, icons done {_done}, re-textured prefabs {FlametalLook.Changed.Count}, rendered {Rendered.Count}");
            if (!inWorld) { _inWorldSince = -1f; if (RuleLog.Due("FlametalIcons.Tick:30", 2d)) ModLog.Every("FlametalIcons.Tick:30", 2d, "FlametalIcons.Tick: returns because no player is in a world (line 30)"); return; }
            if (_inWorldSince < 0f) _inWorldSince = Time.realtimeSinceStartup;
            if (!FlametalIconRules.Due(true, Time.realtimeSinceStartup - _inWorldSince, _done, FlametalLook.Changed.Count)) { if (RuleLog.Due("FlametalIcons.Tick:32", 2d)) ModLog.Every("FlametalIcons.Tick:32", 2d, $"FlametalIcons.Tick: returns because the icons are not due (done {_done}) (line 32)"); return; }
            _done = true;
            foreach (string id in FlametalLook.Changed) RenderOne(id);
        }

        static void RenderOne(string id)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(id);
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (drop == null && piece == null) { ModLog.Verbose($"FlametalIcons.RenderOne: returns because {id} has no inventory or build menu icon (prefab {prefab != null}) (line 42)"); return; }
            Sprite icon = Render(prefab, id);
            Rendered[id] = icon != null;
            if (icon == null) { ModLog.Verbose($"FlametalIcons.RenderOne: returns because the render of {id} gave nothing, it keeps the bundle picture (line 45)"); return; }
            if (drop != null) drop.m_itemData.m_shared.m_icons = new[] { icon };
            if (piece != null) piece.m_icon = icon;
            ModLog.Info($"FlametalIcons: {id} icon rendered from its FlametalNew look ({Size}x{Size}, {(drop != null ? "item" : "piece")})");
        }

        /// <summary>
        /// A bare copy of the prefab's own world model (gp_model, or one a feature moved under a pivot), nothing else: the item's hand copy under "attach"
        /// would be drawn too and shift the framing (gpicons1: two small balls in a corner of the slot). Null when the prefab has no such model.
        /// </summary>
        static GameObject IconSource(GameObject prefab)
        {
            Transform model = prefab.transform.Find("gp_model");
            if (model == null)
                foreach (Transform child in prefab.transform)
                {
                    if (child.name == "attach") { if (RuleLog.IsOn()) ModLog.Verbose($"FlametalIcons.IconSource: skips to the next item (continue) because child.name == \"attach\" (line 61)"); continue; }
                    model = child.Find("gp_model");
                    if (model != null) { if (RuleLog.IsOn()) ModLog.Verbose($"FlametalIcons.IconSource: leaves the loop (break) because model != null (line 63)"); break; }
                }
            if (model == null) { ModLog.Verbose($"FlametalIcons.IconSource: returns null because {prefab.name} has no gp_model (line 65)"); return null; }
            GameObject copy = Object.Instantiate(model.gameObject);
            copy.name = prefab.name + "_icon";
            copy.transform.position = Vector3.zero;
            copy.transform.rotation = model.localRotation;
            { ModLog.Verbose($"FlametalIcons.IconSource: returns a copy of {prefab.name}/{model.parent.name}/gp_model (line 70)"); return copy; }
        }

        static Sprite Render(GameObject prefab, string id)
        {
            GameObject source = null;
            try
            {
                source = IconSource(prefab);
                GameObject target = source != null ? source : prefab;
                Sprite s = RenderManager.Instance.Render(new RenderManager.RenderRequest(target) { Width = Size, Height = Size, UseCache = false });
                if (s == null) ModLog.Warn($"FlametalIcons: the icon render returned nothing for {id}, it keeps the bundle picture");
                double fill = s != null && Measure(s.texture, out int w, out int h, out _, out _) ? System.Math.Max(w, h) / 100d : -1d;
                double distance = FlametalIconRules.Distance(fill);
                if (s != null && distance < 1d)
                {
                    Sprite closer = RenderManager.Instance.Render(new RenderManager.RenderRequest(target) { Width = Size, Height = Size, DistanceMultiplier = (float)distance, UseCache = false });
                    ModLog.Verbose($"FlametalIcons: {id} filled {fill:0.00} of its square, drawn again with the camera at {distance:0.00} ({(closer != null ? "done" : "failed, the first render stays")})");
                    if (closer != null) s = closer;
                }
                { ModLog.Verbose($"FlametalIcons.Render: returns {(s != null ? "a sprite" : "null")} for {id}, drawn from {(source != null ? "its world model only" : "the whole prefab")} (line 90)"); return s; }
            }
            catch (System.InvalidOperationException e)
            {
                ModLog.Warn($"FlametalIcons: the icon render failed for {id} ({e.GetType().FullName}: {e.Message}), it keeps the bundle picture");
                return null;
            }
            catch (System.Exception e)
            {
                ModLog.Warn($"FlametalIcons: unexpected error rendering the icon of {id}, it keeps the bundle picture: {e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                if (source != null) Object.DestroyImmediate(source);
            }
        }

        /// <summary>
        /// Where the visible pixels (alpha above 0.1) of an icon lie, in percent of the square: how much they fill across / up, and how far the middle of
        /// them is off the square's middle. Expected outcome: true with the numbers; false when the pixels cannot be read or none is visible.
        /// </summary>
        internal static bool Measure(Texture2D t, out int fillW, out int fillH, out int offX, out int offY)
        {
            fillW = fillH = offX = offY = 0;
            if (t == null || !t.isReadable) { ModLog.Verbose($"FlametalIcons.Measure: returns false because the texture is {(t == null ? "missing" : "not readable")} (line 115)"); return false; }
            Color32[] px = t.GetPixels32();
            int w = t.width, h = t.height, minX = w, minY = h, maxX = -1, maxY = -1;
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a <= 25) { if (RuleLog.Due("FlametalIcons.Measure:120", 2d)) ModLog.Every("FlametalIcons.Measure:120", 2d, "FlametalIcons.Measure: skips to the next item (continue) because the pixel is see-through (line 120)"); continue; }
                int x = i % w, y = i / w;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            if (maxX < 0) { ModLog.Verbose($"FlametalIcons.Measure: returns false because no pixel of {t.name} is visible (line 127)"); return false; }
            fillW = (maxX - minX + 1) * 100 / w;
            fillH = (maxY - minY + 1) * 100 / h;
            offX = ((minX + maxX + 1) - w) * 50 / w;
            offY = ((minY + maxY + 1) - h) * 50 / h;
            ModLog.Verbose($"FlametalIcons.Measure: returns true, {t.name} fills {fillW}% x {fillH}%, centre off {offX}% {offY}% (line 132)");
            return true;
        }
    }
}
