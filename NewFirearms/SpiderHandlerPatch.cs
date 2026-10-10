using System;
using UnityEngine;
using HarmonyLib;

namespace NewFirearms
{
    // patch to add stun hitbox
    [HarmonyPatch(typeof(SpiderHandler), "Start")]
    class SpiderHandlerPatch
    {
        static void Postfix(SpiderHandler __instance)
        {
            // host does actual logic, so client can not add collider for performence
            if (Plugin.togetherMpEnabled && !Plugin.IsHost())
                return;

            if (!__instance.gameObject.TryGetComponent<CircleCollider2D>(out var origCol))
            {
                UnityEngine.Debug.LogWarning("[NewFirearms] Can not add stun collider to spider, as i dont know original radius");
                return;
            }

            GameObject stunHitbox = new GameObject("StunHitbox");
            stunHitbox.layer = __instance.gameObject.layer;
            stunHitbox.transform.SetParent(__instance.transform);
            stunHitbox.transform.localPosition = Vector3.zero;
            stunHitbox.transform.localScale = Vector3.one;
            CircleCollider2D col = stunHitbox.AddComponent<CircleCollider2D>();
            col.radius = origCol.radius *  WorldGeneration.GetRunSettingFloat("newfirearms.stunhitbox");
            col.isTrigger = true;
        }
    }
}
