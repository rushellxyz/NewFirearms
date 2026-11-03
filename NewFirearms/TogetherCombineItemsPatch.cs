using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NewFirearms
{
    // Together's copied combine routine can be called directly by its server receiver.
    [HarmonyPatch]
    internal static class TogetherCombineItemsPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            if (!Plugin.togetherMpAvailable) return false;
            target = AccessTools.Method("Together.Body_CombineItems_MultiplayerPatch:Body_CombineItems");
            if (target == null)
                Debug.LogWarning("[NewFirearms] Together.Body_CombineItems_MultiplayerPatch:Body_CombineItems not found. Open a bug report");
            return target != null;
        }

        private static MethodBase TargetMethod() => target;

        public static bool Prefix(Body combinerguy, Item it1, Item it2, ref bool __result)
        {
            if (!Plugin.MultiplayerActive || it1 == null || it2 == null) return true;
            if (BodyPatch3.Prefix(combinerguy, it1, it2)) return true;

            // Our loaders send their own client request. Returning false as the combine result
            // also prevents Together from sending a second, native combine request during prediction.
            __result = Plugin.IsHost();
            return false;
        }
    }
}
