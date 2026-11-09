using HarmonyLib;

namespace GunMinigame
{
    [HarmonyPatch(typeof(GunScript), "Fire")]
    internal static class StandaloneShotVisuals
    {
        private static bool Prepare() => Plugin.standalone;
        private static void Postfix(GunScript __instance, bool suicide)
        {
            if (suicide || !__instance.TryGetComponent<StandaloneGun>(out var adapter)) return;
            adapter.FiredDuringUpdate = true;
            var manager = MinigameManager.instance;
            if (manager == null || !manager.active || !ReferenceEquals(manager.gun, adapter) ||
                !adapter.IsLocallyHeld) return;
            float recoil = adapter.HalfRecoil ? __instance.knockBack * 0.5f : __instance.knockBack;
            manager.AddRecoil(__instance.knockBack, recoil);
        }
    }

    [HarmonyPatch(typeof(GunScript), "Update")]
    internal static class StandaloneEjectionVisuals
    {
        private static bool Prepare() => Plugin.standalone;
        private static void Prefix(GunScript __instance, out GunScript.RoundInChamber __state)
        {
            __state = __instance.roundInChamber;
            if (__instance.TryGetComponent<StandaloneGun>(out var adapter))
            {
                adapter.FiredDuringUpdate = false;
                adapter.FilterTriggerInput();
            }
        }
        private static void Postfix(GunScript __instance, GunScript.RoundInChamber __state)
        {
            if (__state == GunScript.RoundInChamber.None ||
                __instance.roundInChamber != GunScript.RoundInChamber.None ||
                !__instance.TryGetComponent<StandaloneGun>(out var adapter)) return;
            var manager = MinigameManager.instance;
            if (manager != null && manager.active && ReferenceEquals(manager.gun, adapter) && adapter.IsLocallyHeld)
                // Fire and ejection can both occur inside this Update: its initial live round is now spent.
                manager.CreateCasing(__state == GunScript.RoundInChamber.Round && !adapter.FiredDuringUpdate ? 0 : -1);
        }
    }
}
