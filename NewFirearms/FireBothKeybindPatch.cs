using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace NewFirearms
{
    [HarmonyPatch(typeof(Settings), "DefaultSettings")]
    internal static class FireBothKeybindPatch
    {
        private const string Name = "newfirearms.fireboth";
        // сообщаю что codex здесь хотел сам парсить json натсройек когда этим игра сама этим занимается
        private static SettingKeybind CreateKeybind()
        {
            var setting = new SettingKeybind
            {
                name = Name,
                value = KeyCode.Mouse4,
                category = Setting.SettingCategory.Input
            };
            setting.apply = () => Plugin.fireBothKey = setting.value;
            return setting;
        }

        private static void Postfix(ref List<Setting> __result)
        {
            if (!__result.Exists(setting => setting.name == Name))
                __result.Add(CreateKeybind());
        }
    }
}
