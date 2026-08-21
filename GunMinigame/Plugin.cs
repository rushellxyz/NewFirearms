using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace GunMinigame
{
    [BepInPlugin("com.rushellxyz.gunminigame", "Gun Minigame", "0.1.0")]
    [BepInDependency("GunsawGenetics", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static bool standalone = false;
        public static bool useMinigame;

        static bool ggEnabled;

        void Awake()
        {
            if (File.Exists("BepInEx/plugins/NewFirearms.dll"))
                Logger.LogInfo("NewFirearms.dll detected, running in api-only mode");
       else if (File.Exists("BepInEx/plugins/GunMinigame-ApiOnlyMode"))
                Logger.LogInfo("GunMinigame-ApiOnlyMode detected, running in api-only mode");
       else if (!Directory.Exists("BepInEx/plugins/GunMinigame/"))
                Logger.LogInfo("GunMinigame/ not detected, running in api-only mode");
       else {
                Logger.LogInfo("Running in standalone mode");
                throw new NotImplementedException();//standalone = true;
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
            ggEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("GunsawGenetics");
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
            texture.LoadImage(fileData);
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
        }

        public static bool IsMarksman(Body body)
        {
            return WorldGeneration.GetRunSettingBool("gunminigame.alwaysmarksman") || (ggEnabled && IsMilkyOrVoyboy(body));
        }

        private static bool IsMilkyOrVoyboy(Body body)
         => false; // TODO
    }

    [HarmonyPatch(typeof(Settings), "DefaultSettings")]
    class SettingsPatch
    {
        static void Postfix(ref List<Setting> __result)
        {
            __result.Add(new SettingBool
            {
                name = "gunminigame.useminigame",
                value = false,
                apply = delegate
                {
                    Plugin.useMinigame = Settings.Get<SettingBool>("gunminigame.useminigame").value;
                },
                category = Setting.SettingCategory.Game,
            });
        }
    }

    [HarmonyPatch(typeof(Locale), "LoadLanguage")]
    class LocalePatch
    {
        static void Postfix()
        {
            Locale.currentLang.other.Add("gamesetgunminigame.useminigame", "Gun minigame");
            Locale.currentLang.other.Add("gamesetgunminigame.useminigamedsc", "Use minigame for gun handling?");
            Locale.currentLang.other.Add("runsetgunminigame.alwaysmarksman", "Always marksman");
            Locale.currentLang.other.Add("runsetgunminigame.alwaysmarksmandsc", "(W.I.P.)");
        }
    }
}
