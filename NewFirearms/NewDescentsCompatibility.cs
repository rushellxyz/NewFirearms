using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace NewFirearms
{
    // Uses vanilla GunScript/AmmoScript APIs. No New Descents types or assembly references.
    internal static class NewDescentsCompatibility
    {
        internal static bool Enabled;

        internal static bool IsMagazinePair(Item gun, Item magazine)
            => Enabled && gun != null && gun.id == "thornslinger" && magazine != null &&
                (magazine.id == "smallmagazine" || magazine.id == "riflemagazine") &&
                gun.GetComponent<GunScript>() != null && magazine.GetComponent<RshMag>() != null;

        internal static bool CanLoad(Item target, Item source)
        {
            if (!IsMagazinePair(target, source)) return false;
            var gun = target.GetComponent<GunScript>();
            var magazine = source.GetComponent<RshMag>();
            if (magazine.prop == null || !magazine.existed || magazine.rounds == null || magazine.rounds.Count == 0)
                return false;
            var caliber = source.id == "smallmagazine" ? GunScript.AmmoType.Pistol : GunScript.AmmoType.Rifle;
            if (gun.hasMag && gun.ammoType == caliber) return false;
            return magazine.rounds.All(round => round == -2 || round >= 0 &&
                round < magazine.prop.ammoTypes.Count);
        }

        internal static void LoadMagazine(Item target, Item source)
        {
            if (!CanLoad(target, source)) return;
            var magazine = source.GetComponent<RshMag>();
            if (Plugin.togetherMpEnabled && magazine.RequestHostIfClient(2, target)) return;
            var gun = target.GetComponent<GunScript>();
            var adapter = source.GetComponent<AmmoScript>() ?? source.gameObject.AddComponent<AmmoScript>();
            adapter.ammoType = source.id == "smallmagazine" ? GunScript.AmmoType.Pistol : GunScript.AmmoType.Rifle;
            adapter.itemType = AmmoScript.AmmoItemType.Magazine;
            adapter.maxRounds = magazine.prop.capacity;
            adapter.rounds = magazine.TotalRounds();
            var roundIds = magazine.rounds.Where(round => round >= 0)
                .Select(round => magazine.prop.ammoTypes[round]).ToList();
            try
            {
                // New Descents' existing LoadMag prefix adopts the caliber and donor weapon stats.
                gun.LoadMag(adapter);
                if (gun.hasMag && gun.ammoType == adapter.ammoType)
                {
                    var contents = target.GetComponent<ThornslingerMagazineContents>() ??
                        target.gameObject.AddComponent<ThornslingerMagazineContents>();
                    contents.MagazineId = source.id;
                    contents.RoundIds = roundIds;
                    magazine.rounds.Clear();
                    magazine.enabled = false;
                }
            }
            finally { UnityEngine.Object.Destroy(adapter); }
        }

        [HarmonyPatch(typeof(GunScript), nameof(GunScript.UnloadMag))]
        private static class UnloadMagazinePatch
        {
            private static bool Prepare() => Enabled;

            private static bool Prefix(GunScript __instance)
            {
                if (!Enabled || !__instance.TryGetComponent<Item>(out var item) || item.id != "thornslinger")
                    return true;
                // Together owns native gun requests; a client must never create ammunition here.
                if (Plugin.togetherMpEnabled && !Plugin.IsHost()) return true;
                if (!__instance.hasMag) return true;
                string id = AmmoScript.AmmoTypeToMagazine(__instance.ammoType);
                if (!Plugin.MagazineDefinitions.TryGetValue(id, out var definition)) return true;
                string roundId = AmmoScript.AmmoTypeToItem(__instance.ammoType);
                int ammoIndex = definition.ammoTypes.IndexOf(roundId);
                if (ammoIndex < 0) return true;
                int count = Mathf.Clamp(__instance.roundsInMag, 0, definition.capacity);
                var rounds = Enumerable.Repeat((sbyte)ammoIndex, count).ToList();
                var stored = __instance.GetComponent<ThornslingerMagazineContents>();
                if (stored != null && stored.MagazineId == id && stored.RoundIds != null && stored.RoundIds.Count >= count)
                {
                    // Native chambering consumes rounds from the front of the magazine.
                    rounds = stored.RoundIds.Skip(stored.RoundIds.Count - count)
                        .Select(round => definition.ammoTypes.IndexOf(round))
                        .Select(index => (sbyte)(index >= 0 ? index : ammoIndex)).ToList();
                }
                rounds.AddRange(Enumerable.Repeat((sbyte)-2, definition.capacity - count));
                var created = Utils.Create(id, __instance.transform.position, 0f);
                var magazineItem = created.GetComponent<Item>();
                // CUCoreLib may add RshMag later in Item.Start. Its initial fill uses condition.
                magazineItem.condition = (float)count / definition.capacity;
                if (created.TryGetComponent<RshMag>(out var magazine))
                {
                    magazine.prop = definition;
                    magazine.existed = true;
                    magazine.rounds = rounds;
                    magazine.UpdateSprite();
                    if (Plugin.togetherMpEnabled) magazine.SyncIfHost();
                }
                else
                {
                    var contents = created.AddComponent<MagazineSpawnContents>();
                    contents.Rounds = rounds;
                }
                if (stored != null)
                {
                    stored.MagazineId = null;
                    stored.RoundIds = null;
                }
                __instance.hasMag = false;
                __instance.roundsInMag = 0;
                Sound.Play("gununloadmag", __instance.transform.position);
                if (__instance.body != null)
                    __instance.body.AutoPickUpItem(magazineItem);
                return false;
            }
        }
    }

    // Carries exact ammunition across CUCoreLib's deferred component initialization.
    internal sealed class MagazineSpawnContents : MonoBehaviour
    {
        internal List<sbyte> Rounds;
    }

    [Saveable]
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class ThornslingerMagazineContents : MonoBehaviour
    {
        [JsonProperty("magazineId")]
        internal string MagazineId;
        [JsonProperty("roundIds")]
        internal List<string> RoundIds;
    }
}
