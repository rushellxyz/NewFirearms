using System.Reflection;
using CUCoreLib.Registries;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NewFirearms
{
    // CUCoreLib snapshots cannot serialize useAction delegates. Replacing a
    // vanilla magazine definition therefore removes unloading on unmodded peers.
    [HarmonyPatch]
    internal static class VanillaMagazineDefinitionsPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            if (!Plugin.nativeClientCompatibilityAvailable) return false;
            target = AccessTools.Method(typeof(ItemRegistry), "CaptureNetworkSnapshot");
            if (target == null)
                Debug.LogWarning("[NewFirearms] Native magazine actions cannot be preserved: CUCoreLib's item snapshot method is unavailable.");
            return target != null;
        }

        private static MethodBase TargetMethod() => target;

        private static void Postfix(JObject __result)
        {
            if (__result == null) return;
            // Leave the native useAction, prefab and metadata on the client.
            // Host definitions and ordinary ammunition-count synchronization stay intact.
            foreach (string id in new[] { "smallmagazine", "riflemagazine", "boxof12gauge" })
                if (Plugin.MagazineDefinitions.ContainsKey(id)) __result.Remove(id);
        }
    }
}
