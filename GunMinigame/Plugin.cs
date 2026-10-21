using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using BepInEx;
using HarmonyLib;

namespace GunMinigame
{
    [BepInPlugin("com.rushellxyz.gunminigame", "Gun Minigame", "0.4.0")]
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
            if (WorldGeneration.GetRunSettingBool("gunminigame.alwaysmarksman"))
                return true;
            if (ggEnabled)
            {
                try
                {
                    return IsMilkyOrVoyboy(body);
                }
           catch{
                    UnityEngine.Debug.LogError("[GunMinigame] Gunsaw Genetics integration failed! Send a bug report.");
                    return false;
                }
            }
       else    return false;
        }

        private static bool IsMilkyOrVoyboy(Body body)
        { // evil string comparassions
            return "Milky" == GunsawGenetics.SpeciesAbilityManager.ActiveSpecies || "Voyager" == GunsawGenetics.SpeciesAbilityManager.ActiveSpecies || GunsawGenetics.SpeciesAbilityManager.MercenaryIsHosting("Milky") || GunsawGenetics.SpeciesAbilityManager.MercenaryIsHosting("Voyager");
        }

        private void PrepareStandaloneMode()
        {
            MinigameManager.bulletToShow = (mag) => {
                AmmoScript ass = mag.GetComponent<AmmoScript>();
                if (null == ass)
                {
                    UnityEngine.Debug.LogWarning("[GunMinigame] Expected item doesnt have a AmmoScript :tourniqet:");
                    return null;
                }
                if (0 == mag.GetComponent<AmmoScript>().rounds)
                    return null;
                return AmmoScript.AmmoTypeToItem(ass.ammoType);

            };
            throw new NotImplementedException();//standalone = true;
        }
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
        [HarmonyAfter("net.cucorelib")]
        static void Postfix()
        {
            ModLocale.ApplyFallback();
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "Update")]
    class MarksmanGunHotkeysPatch
    {
        static void Postfix(PlayerCamera __instance)
        {
            bool rackPressed = Input.GetKeyDown(KeyCode.Z);
            bool ejectPressed = Input.GetKeyDown(KeyCode.X);
            if ((!rackPressed && !ejectPressed) ||
                !Plugin.IsMarksman(__instance.body) ||
                MinigameManager.IsAnyGameUIOpen() ||
                !__instance.body.HoldingItem(__instance.body.handSlot))
                return;

            Item item = __instance.body.GetItem(__instance.body.handSlot);
            if (!item.Stats.HasTag("gun"))
                return;

            MinigameManager manager = MinigameManager.instance;
            IMinigameGun heldGun = item.GetComponent(typeof(IMinigameGun)) as IMinigameGun;
            if (null != manager && manager.active)
            {
                if (ReferenceEquals(heldGun, manager.gun) && !manager.hiden)
                {
                    manager.HandleMarksmanHotkey(rackPressed ? KeyCode.Z : KeyCode.X);
                    return;
                }
                if (!ReferenceEquals(heldGun, manager.gun))
                    manager.Hide();
            }

            if (rackPressed)
                __instance.GunRack();
            else
                __instance.GunEjectMag();
        }
    }
}
