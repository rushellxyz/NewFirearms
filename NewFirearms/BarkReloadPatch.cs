using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace NewFirearms
{
    // Together already relays barking to PantSound.Bark on the host, including
    // peers without this mod. Use that accepted action, not a second key binding.
    [HarmonyPatch(typeof(PantSound), nameof(PantSound.Bark))]
    internal static class BarkReloadPatch
    {
        private sealed class LoadCooldown { internal float Until; }
        private static readonly ConditionalWeakTable<Body, LoadCooldown> cooldowns =
            new ConditionalWeakTable<Body, LoadCooldown>();

        private static void Prefix(PantSound __instance, out Body __state)
        {
            Body body = __instance.GetComponent<Body>();
            __state = body != null && body.conscious && !(body.eatTime > 0f) ? body : null;
        }

        private static void Postfix(Body __state, bool __runOriginal)
        {
            if (__runOriginal && __state != null && __state.eatTime == 0.5f)
                TryLoadHeldItems(__state);
        }

        internal static bool TryLoadHeldItems(Body body)
        {
            if (body == null || !body.alive || !body.conscious ||
                (Plugin.MultiplayerActive && !Plugin.IsHost()) ||
                (body.handSlot != 0 && body.handSlot != 1)) return false;

            Item target = body.GetItem(body.handSlot);
            Item source = body.GetItem(1 - body.handSlot);
            if (target == null || source == null || target == source) return false;

            var gun = target.GetComponent<RshGun>();
            var magazine = target.GetComponent<RshMag>();
            if (gun == null && magazine == null) return false;

            var cooldown = cooldowns.GetValue(body, _ => new LoadCooldown());
            if (Time.unscaledTime < cooldown.Until) return false;

            // Call the authoritative loaders directly. Body.CombineItems would
            // re-enter Together's prediction/announcement path.
            bool loaded = gun != null
                ? gun.prop != null && gun.rounds != null && gun.rounds.Count > 0 && gun.DragOnto(source)
                : magazine.prop != null && magazine.DragOnto(source);
            if (loaded) cooldown.Until = Time.unscaledTime + 0.25f;
            return loaded;
        }
    }
}
