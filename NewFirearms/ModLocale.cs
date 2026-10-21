using System;
using System.Globalization;
using CUCoreLib.Registries;
using UnityEngine;

namespace NewFirearms
{
    internal static class ModLocale
    {
        internal static string Get(string key, params object[] args)
        {
            if (Locale.currentLang == null) Locale.LoadLanguage();
            // The two-argument overload is Get(key, fallback), not Get(category, key).
            string text = LocaleRegistry.Get("other", key, null);
            if (args.Length == 0) return text;
            try
            {
                return string.Format(CultureInfo.CurrentCulture, text, args);
            }
            catch (FormatException ex)
            {
                Debug.LogWarning($"[NewFirearms] Invalid locale format for '{key}': {ex.Message}");
                return text;
            }
        }

        internal static string Course(string key, params object[] args)
            => Get("newfirearms.course." + key, args);
    }
}
