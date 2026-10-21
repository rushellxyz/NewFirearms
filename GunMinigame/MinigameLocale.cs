using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GunMinigame
{
    // CUCoreLib discovers the embedded lang files automatically. Fill missing keys
    // ourselves as well so standalone/API-only installations do not require CUCoreLib.
    internal static class ModLocale
    {
        private static string loadedLanguage;
        private static Dictionary<string, string> fallback;

        internal static void ApplyFallback()
        {
            if (Locale.currentLang == null) return;
            string language = Locale.currentLangName;
            if (fallback == null || loadedLanguage != language)
            {
                fallback = new Dictionary<string, string>();
                LoadEmbedded("EN");
                if (!string.Equals(language, "EN", StringComparison.OrdinalIgnoreCase)) LoadEmbedded(language);
                loadedLanguage = language;
            }
            foreach (var entry in fallback)
                if (!Locale.currentLang.other.TryGetValue(entry.Key, out string text) || string.IsNullOrWhiteSpace(text))
                    Locale.currentLang.other[entry.Key] = entry.Value;
        }

        private static void LoadEmbedded(string language)
        {
            if (string.IsNullOrWhiteSpace(language)) return;
            string resource = "GunMinigame.lang." + language + "-com.rushellxyz.gunminigame.json";
            try
            {
                using (Stream stream = typeof(ModLocale).Assembly.GetManifestResourceStream(resource))
                {
                    if (stream == null) return;
                    using (var reader = new StreamReader(stream))
                    {
                        Language locale = JsonConvert.DeserializeObject<Language>(reader.ReadToEnd());
                        if (locale?.other == null) return;
                        foreach (var entry in locale.other)
                            if (!string.IsNullOrWhiteSpace(entry.Value)) fallback[entry.Key] = entry.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GunMinigame] Failed to load locale '{resource}': {ex.Message}");
            }
        }
    }
}
