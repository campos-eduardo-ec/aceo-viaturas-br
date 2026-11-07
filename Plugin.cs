using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AeroportosBR.Viaturas
{
    [BepInPlugin(Id, "Aeroportos BR - Viaturas e Emergencias", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "br.eduardocampos.aceo.viaturas";
        public const string Version = "1.0.1";
        internal static Plugin Instance;
        internal static ConfigEntry<bool> Enabled, Police, Fire, Ambulance;
        internal static ConfigEntry<string> PoliceMode;
        internal static readonly Dictionary<string, Sprite> Art = new Dictionary<string, Sprite>();
        internal static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        private Harmony harmony;

        private void Awake()
        {
            Instance = this;
            Enabled = Config.Bind("General", "Enabled", true, "Enable Brazilian vehicle appearances. Restart after changing settings.");
            Police = Config.Bind("Vehicles", "Police", true, "PF and Receita Federal liveries on airport police cars. Visual only; same game duties.");
            Fire = Config.Bind("Vehicles", "Fire", true, "Yellow airport fire engine. Original articulated hose is retained.");
            Ambulance = Config.Bind("Vehicles", "Ambulance", true, "White Brazilian airport rescue ambulance.");
            PoliceMode = Config.Bind("Vehicles", "PoliceMode", "Mixed", new ConfigDescription(
                "Mixed chooses a stable livery from the vehicle ID. Rename a vehicle with [PF] or [RF] to choose individually.",
                new AcceptableValueList<string>("Mixed", "PF", "RF")));
            if (!Enabled.Value) { Logger.LogInfo("Viaturas BR disabled by configuration."); return; }
            try
            {
                Load("pf", 320); Load("rf", 320); Load("fire", 102.4f); Load("ambulance", 320);
                harmony = new Harmony(Id);
                harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo("Viaturas BR " + Version + " ready: PF, RF, Fire, Ambulance. Visual-only patches; no save data changes.");
                var diagnosticPath = Environment.GetEnvironmentVariable("ACEO_VIATURAS_TEST_DIR");
                if (!string.IsNullOrEmpty(diagnosticPath)) StartCoroutine(Diagnostics.Run(diagnosticPath));
            }
            catch (Exception ex)
            {
                harmony?.UnpatchSelf();
                Logger.LogError("Viaturas BR could not initialize; vanilla vehicles retained. " + ex);
            }
        }

        private static void Load(string name, float ppu)
        {
            var asm = typeof(Plugin).Assembly;
            var resource = asm.GetManifestResourceNames().Single(n => n.EndsWith(".art." + name + ".png"));
            using (var stream = asm.GetManifestResourceStream(resource))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, buffer.ToArray(), true)) throw new InvalidDataException(name);
                texture.name = "ViaturasBR_" + name;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                Textures.Add(name, texture);
                Art.Add(name, Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), ppu, 0, SpriteMeshType.FullRect));
            }
        }

        internal static bool Supports(VehicleController v) => Enabled.Value &&
            ((Police.Value && v is AirportPoliceCarController) || (Fire.Value && v is FireTruckController) || (Ambulance.Value && v is AmbulanceController));

        internal static void Attach(VehicleController v)
        {
            if (!Supports(v) || v.spriteTransform == null || v.vehicleModel == null) return;
            try
            {
                var skin = v.GetComponent<VehicleSkin>();
                if (skin == null) { skin = v.gameObject.AddComponent<VehicleSkin>(); skin.Setup(v); }
                skin.Apply();
            }
            catch (Exception ex) { Instance.Logger.LogError("Vehicle skin failed: " + ex); }
        }

        internal static void Refresh(VehicleController v)
        {
            try { v.GetComponent<VehicleSkin>()?.Apply(); }
            catch (Exception ex) { Instance.Logger.LogError("Vehicle skin refresh failed: " + ex.Message); }
        }

        internal static string Choice(string name, string id, string mode)
        {
            name = (name ?? "").ToUpperInvariant();
            if (name.Contains("[RF]") || name.Contains("[RFB]")) return "rf";
            if (name.Contains("[PF]")) return "pf";
            if (mode == "RF") return "rf";
            if (mode == "PF") return "pf";
            // FNV-1a avoids .NET's process-dependent string hashing. No new save fields.
            uint hash = 2166136261;
            unchecked { foreach (char c in id ?? "") { hash ^= c; hash *= 16777619; } }
            return (hash & 1) == 0 ? "pf" : "rf";
        }

        internal static void Log(string message) => Instance.Logger.LogInfo(message);
    }

    // Attach after each concrete Launch: police's combined material is initialized last.
    [HarmonyPatch(typeof(AirportPoliceCarController), "Launch")]
    static class PoliceLaunch { static void Postfix(AirportPoliceCarController __instance) => Plugin.Attach(__instance); }
    [HarmonyPatch(typeof(FireTruckController), "Launch")]
    static class FireLaunch { static void Postfix(FireTruckController __instance) => Plugin.Attach(__instance); }
    [HarmonyPatch(typeof(AmbulanceController), "Launch")]
    static class AmbulanceLaunch { static void Postfix(AmbulanceController __instance) => Plugin.Attach(__instance); }
    [HarmonyPatch(typeof(VehicleController), "SetColor")]
    static class ColorPatch { static void Postfix(VehicleController __instance) => Plugin.Refresh(__instance); }
    [HarmonyPatch(typeof(VehicleController), "SetMaterial")]
    static class MaterialPatch { static void Postfix(VehicleController __instance) => Plugin.Refresh(__instance); }

    public sealed class VehicleSkin : MonoBehaviour
    {
        private VehicleController vehicle;
        private SpriteRenderer body, decal, airportLogo, craneCover;
        private Sprite originalSprite;
        private Color originalBodyColor, originalCraneColor;
        private bool originalLogoEnabled;
        private float nextRefresh;
        internal string CurrentLivery;

        internal void Setup(VehicleController v)
        {
            vehicle = v;
            bool police = v is AirportPoliceCarController;
            body = v.spriteTransform.Find(police ? "MainSprite" : "Chassie").GetComponent<SpriteRenderer>();
            originalSprite = body.sprite;
            originalBodyColor = body.color;
            airportLogo = v.spriteTransform.Find("AirportLogo")?.GetComponent<SpriteRenderer>();
            if (airportLogo != null) originalLogoEnabled = airportLogo.enabled;
            if (v is FireTruckController)
            {
                craneCover = v.spriteTransform.Find("CraneBase/ExtensionBase/ExtensionCover")?.GetComponent<SpriteRenderer>();
                if (craneCover != null) originalCraneColor = craneCover.color;
            }
            if (!(v is AmbulanceController))
            {
                var go = new GameObject("ViaturasBR_Decals");
                go.transform.SetParent(v.spriteTransform, false);
                var reference = police ? body : v.spriteTransform.Find("Windows").GetComponent<SpriteRenderer>();
                go.transform.localPosition = reference.transform.localPosition + new Vector3(0,0,-.001f);
                go.transform.localRotation = reference.transform.localRotation;
                go.transform.localScale = reference.transform.localScale;
                decal = go.AddComponent<SpriteRenderer>();
                decal.sortingLayerID = reference.sortingLayerID;
                decal.sortingOrder = reference.sortingOrder + 1;
                v.allSprites = v.spriteTransform.GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        internal void Apply()
        {
            if (vehicle == null || body == null) return;
            if (vehicle is AirportPoliceCarController)
            {
                CurrentLivery = Plugin.Choice(vehicle.vehicleModel.vehicleName, vehicle.vehicleModel.referenceID, Plugin.PoliceMode.Value);
                // Keep the game's combiner: its windows, trim, lights and UV offsets remain intact.
                body.color = CurrentLivery == "pf" ? new Color(.055f,.065f,.075f,1) : Color.white;
                decal.sprite = Plugin.Art[CurrentLivery];
            }
            else if (vehicle is FireTruckController)
            {
                CurrentLivery = "fire";
                body.color = new Color(.86f,.93f,.11f,1);
                if (craneCover != null) craneCover.color = new Color(.70f,.76f,.09f,1);
                decal.sprite = Plugin.Art["fire"];
            }
            else
            {
                CurrentLivery = "ambulance";
                body.sprite = Plugin.Art["ambulance"];
                body.color = Color.white;
            }
            if (airportLogo != null) airportLogo.enabled = false;
            if (decal != null)
            {
                decal.sharedMaterial = SingletonNonDestroy<DataPlaceholderMaterials>.Instance.GetLitNonLitMateral(!vehicle.isInside, false);
                decal.color = Color.white;
                decal.flipX = body.flipX; decal.flipY = body.flipY;
            }
        }

        private void LateUpdate()
        {
            // Per-vehicle name changes need no UI patch, and no scene-wide per-frame scan.
            if (Time.unscaledTime < nextRefresh || vehicle == null) return;
            nextRefresh = Time.unscaledTime + 1f;
            if (vehicle is AirportPoliceCarController)
            {
                var desired = Plugin.Choice(vehicle.vehicleModel.vehicleName, vehicle.vehicleModel.referenceID, Plugin.PoliceMode.Value);
                if (desired != CurrentLivery) Apply();
            }
        }

        private void OnDestroy()
        {
            if (body != null) { body.sprite = originalSprite; body.color = originalBodyColor; }
            if (craneCover != null) craneCover.color = originalCraneColor;
            if (airportLogo != null) airportLogo.enabled = originalLogoEnabled;
            if (decal != null) Destroy(decal.gameObject);
        }
    }
}
