using Newtonsoft.Json;
using UnityEngine;

namespace NewFirearms
{
    [Saveable]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CasingAppearance : MonoBehaviour
    {
        internal const string MessageId = "NewFirearms_CasingAppearance";

        [JsonProperty("gunId")]
        private string gunId;
        [JsonProperty("ammoIndex")]
        private sbyte ammoIndex;

        private Sprite sprite;
        private SpriteRenderer spriteRenderer;

        internal static void Attach(GameObject casing, string definitionId, sbyte index)
        {
            if (!casing.TryGetComponent<CasingAppearance>(out var appearance))
                appearance = casing.AddComponent<CasingAppearance>();
            appearance.gunId = definitionId;
            appearance.ammoIndex = index;
            appearance.ResolveSprite();
        }

        private void Start()
        {
            ResolveSprite();
            if (Plugin.togetherMpEnabled)
                StartSync();
        }

        private void ResolveSprite()
        {
            sprite = null;
            if (!string.IsNullOrEmpty(gunId) && Plugin.GunDefinitions.TryGetValue(gunId, out var gun) &&
                ammoIndex >= 0 && ammoIndex < gun.ammoTypes.Count)
                sprite = gun.ammoTypes[ammoIndex].loadedCasingSprite;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                CUCoreLib.Helpers.CUCoreUtils.SetItemSprite(GetComponent<Item>(), sprite);
                ApplySprite();
            }
        }

        // Item startup and network visual updates can restore the vanilla sprite.
        private void LateUpdate() => ApplySprite();

        private void ApplySprite()
        {
            if (sprite != null && spriteRenderer != null && spriteRenderer.sprite != sprite)
                spriteRenderer.sprite = sprite;
        }

        private void StartSync()
        {
            if (Together.Net.IsServer)
                InvokeRepeating(nameof(Sync), 0.1f, 1f);
        }

        private void Sync()
        {
            if (!Together.Net.IsRunning || !Together.Net.IsServer || string.IsNullOrEmpty(gunId) ||
                !TryGetComponent<Together.SyncInfoGameObjectTracker>(out var tracker) || !tracker.isVisible)
                return;
            var writer = Together.Multiplayer.CreateNamedWriter(MessageId);
            writer.Put((ushort)tracker.syncId);
            writer.Put(gunId);
            writer.Put(ammoIndex);
            Together.Net.Server_SendToClients(LiteNetLib.DeliveryMethod.ReliableUnordered, in writer,
                Together.ServerMain.AllClientIdsExceptHost);
        }

        internal static void ClientSync(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            string definitionId = reader.GetString();
            reader.Get(out sbyte index);
            // Repeat snapshots cover spawn races and players joining after ejection.
            if (Together.ItemSync.TryGetItem(new Together.knetid(syncId), out _, out var item) &&
                item != null && item.id == "casing")
                Attach(item.gameObject, definitionId, index);
        }
    }
}
