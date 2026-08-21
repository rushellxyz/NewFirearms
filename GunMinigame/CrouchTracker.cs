using HarmonyLib;

namespace GunMinigame
{
    [HarmonyPatch(typeof(Body), "Update")]
    static class CrouchTracker
    {
        static bool lastCrouching;

        static void Postfix(Body __instance)
        {
            if (PlayerCamera.main.body != __instance)
                return;

            if (lastCrouching ^ __instance.crouching)
            {
                MinigameManager.GetOrAddInstance().AddRecoil(3f, 3f);
                lastCrouching = __instance.crouching;
            }
        }
    }
}
