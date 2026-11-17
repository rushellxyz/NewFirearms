using System.Runtime.CompilerServices;
using CUCoreLib.Helpers;
using UnityEngine;

namespace NewFirearms
{
    internal static class NativeGameFeatures
    {
        internal static bool HasHearingProtection(Body body)
            => GameBuildInfo.IsFullGame && ReadHearingProtection(body);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadHearingProtection(Body body) => body.HasHearingProtection();

        internal static void UnlockFiringAchievement(Body shooter)
        {
            if (GameBuildInfo.IsFullGame && PlayerCamera.main != null && shooter == PlayerCamera.main.body)
                UnlockPlayingWithFire();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UnlockPlayingWithFire()
            => SteamManager.TryUnlockAchievement("PLAYINGWITHFIRE");

        internal static void ApplyTraderDeathConsequences(Body body)
        {
            if (GameBuildInfo.IsFullGame)
                ApplyFullGameTraderDeathConsequences(body);
            else
                ApplyTraderDeathMood(body, 30f);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ApplyFullGameTraderDeathConsequences(Body body)
        {
            // Match the release's character exemption and mindwipe sickness reduction.
            if (body.charType != 2)
                ApplyTraderDeathMood(body, body.mindWipe ? 15f : 30f);

            SteamManager.TryUnlockAchievement("MYSURVIVALOVERYOURS");
        }

        private static void ApplyTraderDeathMood(Body body, float sickness)
        {
            body.happiness -= 10f;
            body.traumaAmount += 30f;
            body.sicknessAmount += sickness;
            body.StartCoroutine(body.Cry());
        }

        internal static void NotifyScientistHit(GameObject target)
        {
            // The Scientist tag is also absent from the demo.
            if (GameBuildInfo.IsFullGame && target.CompareTag("Scientist")) BeginLabEscape();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BeginLabEscape()
        {
            if (EndLabScript.instance != null) EndLabScript.instance.BeginEscapeSequence();
        }
    }
}
