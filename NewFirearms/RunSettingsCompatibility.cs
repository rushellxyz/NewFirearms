namespace NewFirearms
{
    // Saves created without NewFirearms do not contain its run settings.
    internal static class RunSettingsCompatibility
    {
        internal static bool GetBool(string name, bool fallback)
            => WorldGeneration.runSettings != null &&
               WorldGeneration.runSettings.TryGetValue(name, out var value) && value is bool setting
                ? setting : fallback;

        internal static float GetFloat(string name, float fallback)
            => WorldGeneration.runSettings != null &&
               WorldGeneration.runSettings.TryGetValue(name, out var value) && value is float setting
                ? setting : fallback;
    }
}
