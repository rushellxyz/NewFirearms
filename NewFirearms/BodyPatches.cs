using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace NewFirearms
{
    // patch to make gun usable
    [HarmonyPatch(typeof(Body), "UseItemInHand")]
    public class BodyPatch1
    {
        public static bool Prefix(Body __instance)
        {
            if (!__instance.HoldingItem(__instance.handSlot))
                return true;
            Item it = __instance.GetItem(__instance.handSlot);
            if (!it.Stats.HasTag("gun"))
                return true;
            RshGun rshGun = it.GetComponent<RshGun>();
            if (null == rshGun)
                return true;
            if ((!GunMinigame.Plugin.useMinigame || null == rshGun.prop.minigame || GunMinigame.MinigameManager.GetOrAddInstance().handFaded) && Input.GetKeyDown(KeyBinds.GetBind("attack")))
                rshGun.Shoot();
            return false;
        }
    }

    // patch to make rounds/mag draggable onto gun pt.1
    [HarmonyPatch(typeof(Body), "CanCombine")]
    public class BodyPatch2
    {
        public static bool Prefix(Body __instance, ref bool __result, Item it1, Item it2)
        {
            if (it1.TryGetComponent<RshGun>(out var rshGun) && ((it2.TryGetComponent<RshMag>(out var rshMag) && rshGun.CanFitMag(rshMag.prop)) || rshGun.prop.ammoTypes.Any(ammo => ammo.round == it2.id)) )
            {
                __result = true;
                return false;
            }
        if (it1.TryGetComponent<RshMag>(out var rshMagg) && rshMagg.prop.ammoTypes.Any(ammo => ammo == it2.id))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    // patch to make rounds/mag draggable onto gun pt.2
    [HarmonyPatch(typeof(Body), "CombineItems")]
    public class BodyPatch3
    {
        public static bool Prefix(Body __instance, Item it1, Item it2)
        {
            if (it1.TryGetComponent<RshGun>(out var rshGun))
            {
                rshGun.DragOnto(it2);
                return false;
            }
            if (it1.TryGetComponent<RshMag>(out var rshMag))
            {
                rshMag.DragOnto(it2);
                return false;
            }
            return true;
        }
    }
}
