using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace GunMinigame
{
    public enum PlayerSpecies
    {
        Experiment,
        Milky,
        Dune
    }

    [BepInPlugin("com.rushellxyz.gunminigame", "Gun Minigame", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool standalone = false;
        public static bool useMinigame = true;
        internal const string WindowToggleBind = "gunminigame.togglewindow";

        public static PlayerSpecies GetPlayerSpecies()
            => GetPlayerSpecies(PlayerCamera.main != null ? PlayerCamera.main.body : null);

        public static PlayerSpecies GetPlayerSpecies(Body body)
        {
            // The native character prefabs use charType 0/1/2 for Experiment/Milky/Dune.
            if (body == null) return PlayerSpecies.Experiment;
            switch (body.charType)
            {
                case 1: return PlayerSpecies.Milky;
                case 2: return PlayerSpecies.Dune;
                default: return PlayerSpecies.Experiment;
            }
        }

        void Awake()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rushellxyz.newfirearms") ||
                Directory.GetFiles(Paths.PluginPath, "NewFirearms.dll", SearchOption.AllDirectories).Length != 0)
                Logger.LogInfo("NewFirearms.dll detected, running in api-only mode");
       else if (File.Exists(Path.Combine(Paths.PluginPath, "GunMinigame-ApiOnlyMode")))
                Logger.LogInfo("GunMinigame-ApiOnlyMode detected, running in api-only mode");
       else if (!Directory.Exists(Path.Combine(Paths.PluginPath, "GunMinigame")))
                Logger.LogInfo("GunMinigame/ not detected, running in api-only mode");
       else {
                Logger.LogInfo("Running in standalone mode");
                PrepareStandaloneMode();
            }
            var harmony = new Harmony("com.rushellxyz.gunminigame");
            harmony.PatchAll();
            for (int i = RunSettings.settingTypes.Count - 1; i >= 0; i--)
            {
                if ("encumbrancecap" != RunSettings.settingTypes[i].name)
                    continue;
                RunSettings.settingTypes.Insert(i + 1, new RunSettingBool("gunminigame.alwaysmarksman"));
                break;
            }
            RunSettings.presets[0].presetValues.Add("gunminigame.alwaysmarksman", false);
            RunSettings.presets[1].presetValues.Add("gunminigame.alwaysmarksman", false);
            RunSettings.presets[2].presetValues.Add("gunminigame.alwaysmarksman", true);
            RunSettings.presets[3].presetValues.Add("gunminigame.alwaysmarksman", false);
            RunSettings.presets[4].presetValues.Add("gunminigame.alwaysmarksman", false);
        }

        public static Sprite LoadSprite(string path, float ppu=8.0f)
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(8, 8);
            if (!texture.LoadImage(fileData))
            {
                Destroy(texture);
                throw new InvalidDataException("Could not decode PNG: " + path);
            }
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
        }

        internal static Sprite LoadResource(string name)
            => LoadSprite(Path.Combine(Paths.PluginPath, standalone ? "GunMinigame" : "NewFirearms",
                "Resources", name + ".png"));

        public static bool IsMarksman(Body body)
            => WorldGeneration.GetRunSettingBool("gunminigame.alwaysmarksman") ||
                GetPlayerSpecies(body) == PlayerSpecies.Milky;

        private void PrepareStandaloneMode()
        {
            string root = Path.Combine(Paths.PluginPath, "GunMinigame");
            StandaloneContent.Load(root, path => LoadSprite(path), message => Logger.LogWarning(message));
            standalone = StandaloneContent.Guns.Count != 0;
            Logger.LogInfo("Standalone minigames loaded: " + StandaloneContent.Guns.Count);
            MinigameManager.bulletToShow = (mag) => {
                AmmoScript ass = mag.GetComponent<AmmoScript>();
                if (null == ass)
                {
                    UnityEngine.Debug.LogWarning("[GunMinigame] Expected item doesnt have a AmmoScript :tourniqet:");
                    return null;
                }
                if (ass.rounds <= 0)
                    return null;
                return AmmoScript.AmmoTypeToItem(ass.ammoType);

            };
        }
    }

    [HarmonyPatch(typeof(Settings), "DefaultSettings")]
    class SettingsPatch
    {
        static void Postfix(ref List<Setting> __result)
        {
            if (!__result.Exists(setting => setting.name == "marksmanrack"))
                __result.Add(new SettingKeybind { name = "marksmanrack", value = KeyCode.E, category = Setting.SettingCategory.Input });
            if (!__result.Exists(setting => setting.name == "marksmanunload"))
                __result.Add(new SettingKeybind { name = "marksmanunload", value = KeyCode.G, category = Setting.SettingCategory.Input });
            __result.Add(new SettingBool
            {
                name = "gunminigame.useminigame",
                value = true,
                apply = delegate
                {
                    Plugin.useMinigame = Settings.Get<SettingBool>("gunminigame.useminigame").value;
                },
                category = Setting.SettingCategory.Game,
            });
            __result.Add(new SettingKeybind
            {
                name = Plugin.WindowToggleBind,
                value = KeyCode.H,
                category = Setting.SettingCategory.Input,
            });
        }
    }

    [HarmonyPatch(typeof(Locale), "LoadLanguage")]
    class LocalePatch
    {
        [HarmonyAfter("net.cucorelib")]
        static void Postfix()
        {
            ModLocale.ApplyFallback();
        }
    }

}
