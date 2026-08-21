using UnityEngine;
using HarmonyLib;

namespace GunMinigame
{
    [HarmonyPatch(typeof(Body), "SwitchHands")]
    static class SwitchHandsTracker
    {
        static void Postfix()
        {
            var mm = MinigameManager.GetOrAddInstance();
            mm.shouldntRefreshBandolierCount = false;
//            mm.handTransform.localScale = new Vector3(-mm.handTransform.localScale.x, mm.handTransform.localScale.y);
        }
    }
}
