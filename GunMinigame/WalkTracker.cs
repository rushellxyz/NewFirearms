using HarmonyLib;

namespace GunMinigame
{
    [HarmonyPatch(typeof(Body), "FootStep")]
    class WalkTracker
    {
        static void Postfix(Body __instance, float vol)
        {
            if (__instance != PlayerCamera.main.body)
                return;

            MinigameManager.instance?.AddFootstepImpulse(vol);
        }
    }
}
