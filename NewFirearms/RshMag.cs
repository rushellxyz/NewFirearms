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
            if (Plugin.togetherMpEnabled)
                SyncIfHost(2);
        }

        public int TotalRounds()
        {
            return rounds == null ? 0 : rounds.Count(round => 0 <= round);
        }

        public void Fill(sbyte type, int amount)
        {
            amount = Mathf.Clamp(amount, 0, prop.capacity);
            rounds = Enumerable.Repeat(type, amount).ToList();
            rounds.AddRange(Enumerable.Repeat((sbyte)-2, prop.capacity - amount));
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

        public static void ClientSync(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out ushort roundsCount);
            if (0 == roundsCount)
                throw new Exception("[NewFirearms] Attempt to initalize rounds with count of 0!");
            var snapshot = new List<sbyte>(roundsCount);
            for (ushort i = 0; i < roundsCount; i++)
            {
                reader.Get(out sbyte newRound);
                snapshot.Add(newRound);
            }
            reader.Get(out byte extraData);
            // Periodic packets can race item spawn/despawn. A later snapshot retries.
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var _, out Item it) || it == null ||
                !it.TryGetComponent<RshMag>(out var rshMag)) return;
            rshMag.rounds = snapshot;
            rshMag.existed = true;
            if (rshMag.prop == null) rshMag.LoadDefinition();
            if (rshMag.prop == null) return;
            if (1 == extraData)
                Sound.Play(rshMag.prop.loadRoundAudio, rshMag.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (2 == extraData)
                Sound.Play(rshMag.prop.unloadRoundAudio, rshMag.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
        }

        public static readonly Dictionary<byte, string> ACTIONS_NAME = new Dictionary<byte, string>
        {
            {0, "newfirearms.action.remove_round"},
            {1, "newfirearms.action.load_round"},
        };

        public static void ServerReceiver(Together.ScavPlayer plr, LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out byte action);

            if (2 <= action)
            {
                DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.unknown"));
                return;
            }
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var si, out var item))
            {
                DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.unregistered", ModLocale.Get(ACTIONS_NAME[action])));
                return;
            }
            if (!item.TryGetComponent<RshMag>(out var rshMag))
            {
                DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.invalid", ModLocale.Get(ACTIONS_NAME[action])));
                return;
            }
            if (!plr.TryGetNetBody(out var pb))
            {
                DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.noBody", ModLocale.Get(ACTIONS_NAME[action])));
                return;
            }
            if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(si, pb.body, check_obstruction:true))
            {
                DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.outOfReach", ModLocale.Get(ACTIONS_NAME[action])), canBeFixedWithSwitchingStrictSync:true);
                return;
            }

            if (0 == action)
                rshMag.RemoveRound(pb.body);
       else if (1 == action)
            {
                reader.Get(out ushort ontoSyncId);
                if (!Together.ItemSync.TryGetItem(new Together.knetid(ontoSyncId), out var ontoSi, out var ontoItem))
                {
                    DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.roundUnregistered"));
                    return;
                }
                if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(ontoSi, pb.body, check_obstruction:true))
                {
                    DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.roundOutOfReach"), canBeFixedWithSwitchingStrictSync:true);
                    return;
                }
                rshMag.DragOnto(ontoItem);
            }
        }
    }
}
