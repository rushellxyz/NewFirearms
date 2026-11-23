using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NewFirearmsManage
{
    [BepInPlugin(Id, "New Firearms Manage", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const string Id = "com.rushellxyz.newfirearmsmanage";
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;
            new Harmony(Id).PatchAll();
        }
    }

    // The menu uses this list to build its controls. Keep the toggle out of saved settings.
    [HarmonyPatch(typeof(Settings), nameof(Settings.GetAllSettings))]
    internal static class ArsenalSetting
    {
        private const string SettingName = "newfirearmsmanage.extendarsenal";
        private const string EnabledFile = "NewFirearms.dll";
        private const string DisabledFile = "NewFirearms.dll.disabled";

        private static void Postfix(ref List<Setting> __result)
        {
            string enabledPath = Path.Combine(Paths.PluginPath, EnabledFile);
            string disabledPath = Path.Combine(Paths.PluginPath, DisabledFile);
            bool enabled = File.Exists(enabledPath);
            if (!enabled && !File.Exists(disabledPath)) return;

            if (Locale.currentLang == null) Locale.LoadLanguage();
            var strings = Locale.currentLang?.other;
            if (strings != null)
            {
                strings["gameset" + SettingName] = "Extend vanilla arsenal?";
                strings["gameset" + SettingName + "dsc"] =
                    "If disabled, New Firearms will not add new weapons. Restart the game to apply.";
            }

            var setting = new SettingBool
            {
                name = SettingName,
                category = Setting.SettingCategory.Game,
                value = enabled,
            };
            setting.apply = () => Apply(setting, enabledPath, disabledPath);
            __result = new List<Setting>(__result) { setting };
        }

        private static void Apply(SettingBool setting, string enabledPath, string disabledPath)
        {
            string source = setting.value ? disabledPath : enabledPath;
            string destination = setting.value ? enabledPath : disabledPath;
            if (!File.Exists(source) && File.Exists(destination)) return;

            try
            {
                File.Move(source, destination);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Plugin.Log?.LogWarning($"Could not rename '{source}' to '{destination}': {exception.Message}");
                setting.value = File.Exists(enabledPath);
            }
        }
    }
}
