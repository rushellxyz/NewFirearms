using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace NewFirearms
{
    // semi-override to add new guns/mags to milky sell menu
    // prostethics mod adds chips to them in postfix, so uhh it shouldn't conflict
    [HarmonyPatch(typeof(TraderScript), "GenerateInventory")]
    public class TraderInventoryPatch
    {
        static bool Prefix(TraderScript __instance)
        {
            if (1 != __instance.character)
                return true;

            __instance.items = new List<TraderItem>();
            __instance.items.AddRange(__instance.GenerateSingleItemList(TraderScript.TraderItemPreference.WantsTrade));
            __instance.items.AddRange(__instance.GenerateSingleItemList(TraderScript.TraderItemPreference.Indifferent));
            __instance.items.AddRange(__instance.GenerateSingleItemList(TraderScript.TraderItemPreference.WantsKeep));
            for (int i = 0; i < 2; i++)
            {
                string id = Plugin.milkyMags.PickRandom();
                __instance.items.Add(new TraderItem
                {
                    preference = (TraderScript.TraderItemPreference)UnityEngine.Random.Range(0, 3),
                    id = id,
                    value = Item.GetItem(id).value
                });
            }
            string id2 = Plugin.milkyGuns.PickRandom();
            __instance.items.Add(new TraderItem
            {
                preference = (TraderScript.TraderItemPreference)UnityEngine.Random.Range(0, 3),
                id = id2,
                value = Item.GetItem(id2).value
            });
            __instance.items = __instance.items.OrderBy((TraderItem x) => (int)x.preference).ToList();
            return false;
        }
    }
}
