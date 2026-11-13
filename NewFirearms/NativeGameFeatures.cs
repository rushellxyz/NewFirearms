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
