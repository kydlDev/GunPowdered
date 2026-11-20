using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// The fire behind the see-through panes of the Flametal Heater (Leaching Station) and the Flametal Drying Oven (Powder Mill), D-138: while a station
    /// of its kind within <see cref="BatchStationExtension.Range"/> m runs, the model part "fire" (emissive flame shapes) is shown, flickers, and a
    /// flickering orange point light burns inside. No particles, so no smoke (user: "if possible without smoke"). Every client decides for itself from
    /// the station's ZDO state (BatchStation.AnyRunningNear), so every player sees the same fire. A build ghost never burns.
    /// </summary>
    internal sealed class ExtensionFire : MonoBehaviour
    {
        const float CheckSeconds = 1f, LightRange = 3f, LightIntensity = 2.2f;
        static readonly Color FireColour = new Color(1f, 0.55f, 0.18f);

        string _station;

        /// <summary>True while the fire burns (a station of its kind within range runs).</summary>
        internal bool Burning { get { return _burning; } }

        /// <summary>True when the model part "fire" is shown.</summary>
        internal bool FlamesShown { get { return _fire != null && _fire.activeSelf; } }

        /// <summary>The fire light's intensity now; 0 when it is off or not made yet.</summary>
        internal float LightNow { get { return _light != null && _light.enabled ? _light.intensity : 0f; } }

        GameObject _fire;
        Light _light;
        Vector3 _fireScale = Vector3.one;
        bool _burning;
        float _nextCheck, _seed;

        void Start()
        {
            var extension = GetComponent<BatchStationExtension>();
            _station = extension != null ? extension.Station : null;
            _fire = FindFire(transform);
            _seed = UnityEngine.Random.value * 100f;
            if (_fire != null)
            {
                _fireScale = _fire.transform.localScale;
                _fire.SetActive(false);
            }
            ModLog.Check("extension fire: model part 'fire' found on " + Utils.GetPrefabName(gameObject), _fire != null ? 1 : 0, 1, 0f, "parts");
            if (RuleLog.IsOn()) ModLog.Verbose($"ExtensionFire.Start({Utils.GetPrefabName(gameObject)}): station {_station}, fire part {_fire != null}");
        }

        static GameObject FindFire(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "fire") { if (RuleLog.IsOn()) ModLog.Verbose("ExtensionFire.FindFire: returns the 'fire' part"); return t.gameObject; }
            }
            if (RuleLog.IsOn()) ModLog.Verbose("ExtensionFire.FindFire: returns null (no 'fire' part)");
            return null;
        }

        void Update()
        {
            try
            {
                if (_fire == null || _station == null || Player.IsPlacementGhost(gameObject)) { if (RuleLog.IsOn()) ModLog.Every("ExtensionFire.Update:none", 2d, "ExtensionFire.Update: no fire part or a build ghost - returns"); return; }
                if (Time.time >= _nextCheck)
                {
                    _nextCheck = Time.time + CheckSeconds;
                    SetBurning(BatchStation.AnyRunningNear(_station, transform.position, BatchStationExtension.Range));
                }
                if (_burning) Flicker();
            }
            catch (Exception e)
            {
                ModLog.Warn($"ExtensionFire.Update: {e.GetType().FullName}: {e.Message} - the fire keeps its last state this frame");
            }
        }

        void SetBurning(bool burning)
        {
            if (burning == _burning) { if (RuleLog.IsOn()) ModLog.Every("ExtensionFire.SetBurning:same", 2d, $"ExtensionFire.SetBurning({burning}): unchanged - returns"); return; }
            _burning = burning;
            _fire.SetActive(burning);
            if (burning && _light == null) _light = MakeLight();
            if (_light != null) _light.enabled = burning;
            ModLog.State("extension fire " + Utils.GetPrefabName(gameObject), burning ? "out" : "burning", burning ? "burning" : "out", (burning ? "a " : "no ") + _station + " within " + BatchStationExtension.Range + " m runs");
        }

        Light MakeLight()
        {
            var go = new GameObject("fire_light");
            go.transform.SetParent(_fire.transform.parent, false);
            go.transform.position = _fire.transform.position + Vector3.up * 0.25f;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = FireColour;
            light.range = LightRange;
            light.intensity = LightIntensity;
            light.shadows = LightShadows.None;
            if (RuleLog.IsOn()) ModLog.Verbose($"ExtensionFire.MakeLight: point light {LightRange} m, intensity {LightIntensity}");
            return light;
        }

        // the flames stretch and the light breathes with smooth noise (no allocation, cheap per frame)
        void Flicker()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * 3.5f);
            _fire.transform.localScale = new Vector3(_fireScale.x * (0.95f + 0.1f * n), _fireScale.y * (0.8f + 0.4f * n), _fireScale.z);
            if (_light != null) _light.intensity = LightIntensity * (0.75f + 0.5f * n);
            ModLog.Every("extension-fire-" + gameObject.GetInstanceID(), 30d, $"extension fire {Utils.GetPrefabName(gameObject)} burning, flicker {n:0.00}, light {(_light != null ? _light.intensity : 0f):0.00}");
        }
    }
}
