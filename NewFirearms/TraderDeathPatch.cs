using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NewFirearms
{
    [HarmonyPatch(typeof(TraderScript), "Update")]
    [HarmonyPriority(Priority.First)] //
    static class TraderDeathPatch
    {
        static void Prefix(TraderScript __instance)
        {
            if (__instance.build.health >= 200f)
                return;
            if (__instance.didDeathMoodDebuff)
                return;

            __instance.sitTime = Time.time;
            __instance.standTarget = -0.1f;
            __instance.eyes.sprite = __instance.eyeSprites[0];

            __instance.DropInventory();
            __instance.didDeathMoodDebuff = true;

            Body body = PlayerCamera.main.body;
            body.skills.AddExp(1, 60f);
            body.happiness -= 10f;
            body.traumaAmount += 30f;
            body.sicknessAmount += 30f;
            body.StartCoroutine(body.Cry());
            body.talker.Talk(Locale.GetCharacter("murderregret"));
            __instance.build.description = Locale.GetBuilding("traderdscdead");
            __instance.build.fullName = Locale.GetBuilding("corpseseen");
            UnityEngine.Object.Destroy(__instance.GetComponent<UsableObject>());
        }

    }
}
