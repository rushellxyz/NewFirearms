using HarmonyLib;
using UnityEngine;

namespace GunMinigame
{
    [HarmonyPatch(typeof(Settings), "LoadSettings")]
    internal static class MinigameSettingsMigration
    {
        private const string MigrationKey = "GunMinigame.UseMinigameDefaultEnabled.v1";

        private static void Postfix()
        {
            // Read the loaded list directly; Settings.Get can reload settings on a fresh install.
            var setting = Settings.settings?.Find(value => value.name == "gunminigame.useminigame") as SettingBool;
            if (setting == null) return;

            if (PlayerPrefs.GetInt(MigrationKey, 0) == 0)
            {
                setting.value = true;
                Settings.SaveSettings();
                PlayerPrefs.SetInt(MigrationKey, 1);
                PlayerPrefs.Save();
            }
            Plugin.useMinigame = setting.value;
        }
    }
}
