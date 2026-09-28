using HarmonyLib;

namespace GunMinigame
{
    [HarmonyPatch(typeof(PlayerCamera), "HandleGunMenu")]
    class StandalnoeHandler
    {
        static void Postfix(PlayerCamera __instance)
        {
            __instance.gunMenu.SetActive(value: false);
            //gunCrosshair.gameObject.SetActive(!component.safe);
        }

        static bool Prepare()
         => Plugin.standalone;
    }
}
