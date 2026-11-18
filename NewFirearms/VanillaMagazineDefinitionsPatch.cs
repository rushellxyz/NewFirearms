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

            // Custom magazines have no native unload delegate on network-only peers.
            // Disable the advertised action, not the host's registered definition.
            foreach (string id in Plugin.MagazineDefinitions.Keys)
                if (__result[id] is JObject magazine) magazine["usable"] = false;
        }
    }

    [HarmonyPatch]
    internal static class LocalMagazineActionsPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            target = AccessTools.Method(typeof(ItemRegistry), "ApplyNetworkSnapshot");
            if (target == null)
                Debug.LogWarning("[NewFirearms] Local magazine actions cannot be preserved: CUCoreLib's item snapshot receiver is unavailable.");
            return target != null;
        }

        private static MethodBase TargetMethod() => target;

        private static void Prefix(ref JObject __0)
        {
            if (__0 == null) return;
            JObject localSnapshot = null;
            foreach (string id in Plugin.MagazineDefinitions.Keys)
            {
                if (!(__0[id] is JObject) ||
                    !ItemRegistry.TryGetCustomInfo(id, out var localInfo) || localInfo == null || localInfo.useAction == null)
                    continue;

                // CUCoreLib retains local delegates when applying a snapshot. Restore
                // usability only when this peer has the corresponding local action.
                if (localSnapshot == null) localSnapshot = (JObject)__0.DeepClone();
                localSnapshot[id]["usable"] = localInfo.usable;
            }
            if (localSnapshot != null) __0 = localSnapshot;
        }
    }
}
