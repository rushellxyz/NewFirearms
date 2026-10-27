using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Newtonsoft.Json;

namespace NewFirearms
{
    // MonoBehaviour script for each individual mag
    [Saveable]
    [JsonObject(MemberSerialization.OptIn)]
    public class RshMag : RshComponent, GunMinigame.IMinigameMag
    {
        public const string MSGID_SYNC = "NewFirearms_RshMagSync";
        public const string MSGID_ACTION = "NewFirearms_RshMagAction";

        public MagJson prop;

        [JsonProperty("rounds")]
        public List<sbyte> rounds = new List<sbyte>();
        [JsonProperty("existed")]
        public bool existed;

        public override void LoadDefinition()
        {
            Item item = GetComponent<Item>();
            if (item == null || string.IsNullOrEmpty(item.id)) return;
            it = item;
            Plugin.MagazineDefinitions.TryGetValue(item.id, out prop);
        }

        private void Start()
        {
            if (prop == null) LoadDefinition();
            if (prop == null || prop.capacity <= 0 || prop.ammoTypes == null || prop.ammoTypes.Count == 0)
            {
                Debug.LogError($"[NewFirearms] Magazine '{it?.id}' has no valid local definition; magazine initialization stopped.");
                return;
            }
            if (rounds == null) rounds = new List<sbyte>();
            // Only the host creates initial ammunition; clients wait for its snapshot.
            if (!existed && (!Plugin.togetherMpEnabled || Plugin.IsHost()))
            {
                sbyte ammoType = (sbyte)UnityEngine.Random.Range(0, prop.ammoTypes.Count);
                int amount = Mathf.RoundToInt(prop.capacity * Mathf.Clamp01(it.condition));
                Fill(ammoType, amount);
                it.condition = 1f;
                existed = true;
            }
            if (Plugin.togetherMpEnabled)
                MpStartOp();
            UpdateSprite();
        }

        public void MpStartOp()
        {
            if (Together.Net.IsServer)
            {
                InvokeRepeating("SyncIfHostt", Plugin.sett.magSyncRate, Plugin.sett.magSyncRate);
            }
        }

        public bool DragOnto(Item item)
        {
            if (item == null || prop == null) return false;

            sbyte round = (sbyte)prop.ammoTypes.IndexOf(item.id);
            if (-1 == round)
                return false;

            if (Plugin.togetherMpEnabled && RequestHostIfClient(1, item))
                return true;
            if (rounds == null || rounds.Count == 0 || -2 != rounds[rounds.Count - 1])
                return false;

            Sound.Play(prop.loadRoundAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            Plugin.ShiftRight(ref rounds);
            rounds[0] = round;
            UpdateSprite();
            UnityEngine.Object.Destroy(item.gameObject);
            if (Plugin.togetherMpEnabled)
                SyncIfHost(1);
            return true;
        }

        public void OnInventoryUse(Body body)
        {
            // Together forwards inventory use to the host and relays it to clients.
            // Only the host should unload; sending our own request would do it twice.
            if (Plugin.togetherMpEnabled && !Plugin.IsHost()) return;
            RemoveRound(body);
        }

        public void RemoveRound(Body body=null)
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(0))
                return;
            if (prop == null || rounds == null || rounds.Count == 0 || -2 == rounds[0])
                return;
            Sound.Play(prop.unloadRoundAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            if (null == body)
                body = PlayerCamera.main.body;
            string round = "casing";
            if (0 <= rounds[0])
                round = prop.ammoTypes[rounds[0]];
       else     UnityEngine.Debug.LogWarning("Removing casing from magazine :hmm:");
            body.AutoPickUpItem(Utils.Create(round, transform.position, 0f).GetComponent<Item>());
            rounds[0] = -2;
            Plugin.ShiftLeft(ref rounds);
            UpdateSprite();
            if (Plugin.togetherMpEnabled)
                SyncIfHost(2);
        }

        public int TotalRounds()
        {
            return rounds == null ? 0 : rounds.Count(round => 0 <= round);
        }

        public void UpdateSprite()
        {
            if (prop == null || prop.nonEmptySprite == null) return;
            if (TryGetComponent<SpriteRenderer>(out var renderer))
                renderer.sprite = TotalRounds() > 0 ? prop.nonEmptySprite : prop.emptySprite;
        }

        public void Fill(sbyte type, int amount)
        {
            amount = Mathf.Clamp(amount, 0, prop.capacity);
            rounds = Enumerable.Repeat(type, amount).ToList();
            rounds.AddRange(Enumerable.Repeat((sbyte)-2, prop.capacity - amount));
            UpdateSprite();
            if (Plugin.togetherMpEnabled && existed)
                SyncIfHost();
        }

        public void SyncIfHostt()
         => SyncIfHost(reliable: false);

         /*
          * 1 - Play load round
          * 2 - Play unload round
          */
        public override void SyncIfHost(byte extraData=0, bool reliable=true)
        {
            if (!Together.Net.IsServer)
                return;
            if (rounds == null || rounds.Count == 0) return;
            Together.SyncInfoGameObjectTracker tracker = (GetMpTracker(reliable) as Together.SyncInfoGameObjectTracker);
            if (null == tracker)
                return;
            if (!tracker.isVisible)
                return;
            LiteNetLib.Utils.NetDataWriter writer = Together.Multiplayer.CreateNamedWriter(MSGID_SYNC);
            writer.Put((ushort)tracker.syncId);

            writer.Put((ushort)rounds.Count());
            for (ushort i = 0; i < rounds.Count(); i++)
                writer.Put((sbyte)rounds[i]);

            writer.Put((byte)extraData);

            LiteNetLib.DeliveryMethod method;
            if (reliable)
                method = LiteNetLib.DeliveryMethod.ReliableUnordered;
       else     method = LiteNetLib.DeliveryMethod.Unreliable;
            Together.Net.Server_SendToClients(method, in writer, Together.ServerMain.AllClientIdsExceptHost);
        }

        public bool RequestHostIfClient(byte action, Item second=null)
        {
            if (Together.Net.IsServer)
                return false;
            LiteNetLib.Utils.NetDataWriter writer = Together.Multiplayer.CreateNamedWriter(MSGID_ACTION);
            Together.SyncInfoGameObjectTracker tracker = (GetMpTracker(true) as Together.SyncInfoGameObjectTracker);
            writer.Put((ushort)tracker.syncId);
            writer.Put((byte)action);
            if (null != second)
            {
                if (!second.TryGetComponent<Together.SyncInfoGameObjectTracker>(out var ksmgont2))
                    throw new Exception("[NewFirearms] Attempt to mag action on drag onto without ScavMultiGameObjectNetworkTracker :hmm:");
                writer.Put((ushort)ksmgont2.syncId);
            }
            Together.Net.Client_Send(LiteNetLib.DeliveryMethod.ReliableUnordered, in writer);
            return true;
        }


        public static readonly Dictionary<byte, string> ACTIONS_NAME = new Dictionary<byte, string>
        {
            {0, "newfirearms.action.remove_round"},
            {1, "newfirearms.action.load_round"},
        };
    }
}
