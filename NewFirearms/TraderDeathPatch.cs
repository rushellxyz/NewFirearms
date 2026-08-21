using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NewFirearms
{
    [HarmonyPatch(typeof(BuildingEntity), "Update")]
    [HarmonyPriority(Priority.First)] //
    static class TraderDeathPatch
    {
        static void Prefix(BuildingEntity __instance)
        {
            if (__instance.health >= 200f)
                return;
            if (!__instance.TryGetComponent<TraderScript>(out var trader))
                return;
            if (trader.didDeathMoodDebuff)
                return;

            trader.sitTime = Time.time;
            trader.standTarget = -0.1f;
            trader.eyes.sprite = trader.eyeSprites[0];

            trader.DropInventory();
            trader.didDeathMoodDebuff = true;

            Body body = PlayerCamera.main.body;
            body.skills.AddExp(1, 60f);
            body.happiness -= 10f;
            body.traumaAmount += 30f;
            body.sicknessAmount += 30f;
            body.StartCoroutine(body.Cry());
            body.talker.Talk(Locale.GetCharacter("murderregret"));
            __instance.description = Locale.GetBuilding("traderdscdead");
            __instance.fullName = Locale.GetBuilding("corpseseen");
            UnityEngine.Object.Destroy(trader.GetComponent<UsableObject>());
        }

    }
}
