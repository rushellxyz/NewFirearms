using UnityEngine;
using HarmonyLib;

namespace NewFirearms
{
    // patch to fix magazines always having maximum trading value
    [HarmonyPatch(typeof(ItemInfo), "GetValue")]
    class MagazineValuePatch
    {
        static void Postfix(ItemInfo __instance, ref int __result, Item item)
        {
            if (item.TryGetComponent<RshMag>(out var rshMag))
            {
                __result = Mathf.RoundToInt((float)__result * ((float)rshMag.TotalRounds() / (float)rshMag.prop.capacity));
            }
        }
    }
}
