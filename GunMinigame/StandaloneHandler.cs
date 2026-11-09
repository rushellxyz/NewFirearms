using HarmonyLib;

namespace GunMinigame
{
    [HarmonyPatch(typeof(PlayerCamera), "HandleGunMenu")]
    internal static class StandaloneHandler
    {
        static void Postfix(PlayerCamera __instance)
        {
            if (__instance != PlayerCamera.main) return;
            var body = __instance.body;
            var item = body != null ? body.GetItem(body.handSlot) : null;
            if (!Plugin.useMinigame || item == null ||
                !StandaloneContent.Guns.TryGetValue(item.id, out var info) ||
                !item.TryGetComponent<GunScript>(out var gun))
            {
                var current = MinigameManager.instance;
                if (current != null && current.active && current.gun is StandaloneGun) current.Hide();
                return;
            }
            if (!item.TryGetComponent<StandaloneGun>(out var adapter))
                adapter = item.gameObject.AddComponent<StandaloneGun>();
            adapter.Initialize(gun, item, info);
            MinigameManager.GetOrAddInstance().Show(info, adapter);
            __instance.gunMenu.SetActive(false);
            // Keep the vanilla crosshair and weapon state.
        }

        static bool Prepare()
         => Plugin.standalone;
    }
}
