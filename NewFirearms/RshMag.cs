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
    public class RshMag : RshComponent
    {
        public const string MSGID_SYNC = "NewFirearms_RshMagSync";
        public const string MSGID_ACTION = "NewFirearms_RshMagAction";

        public MagJson prop;

        [JsonProperty("rounds")]
        public List<sbyte> rounds;
        [JsonProperty("existed")]
        public bool existed;


        public override void LoadPropFromCuCore()
        {
            CUCoreLib.Registries.ItemRegistry.TryGetCustomData<MagJson>(GetComponent<Item>(), "prop", out prop);
        }

        void Start()
        {
            if (existed)
                return;
            Fill((sbyte)UnityEngine.Random.Range(0, prop.ammoTypes.Count), Mathf.RoundToInt(prop.capacity * it.condition));
            it.condition = 1f;
            if (Plugin.krokMpEnabled)
                MpStartOp();
            existed = true;
        }

        public void MpStartOp()
        {
            if (KrokoshaCasualtiesMP.Net.is_server)
            {
                InvokeRepeating("SyncIfHostt", Plugin.sett.magSyncRate, Plugin.sett.magSyncRate);
            }
        }

        public void DragOnto(Item item)
        {
            if (-2 != rounds[rounds.Count() - 1])
                return;

            sbyte round = (sbyte)prop.ammoTypes.IndexOf(item.id);
            if (-1 == round)
                return;

            if (Plugin.krokMpEnabled && RequestHostIfClient(1, item))
                return;

            Sound.Play(prop.loadRoundAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            Plugin.ShiftRight(ref rounds);
            rounds[0] = round;
            UnityEngine.Object.Destroy(item.gameObject);
            if (Plugin.krokMpEnabled)
                SyncIfHost(1);
        }

        public void RemoveRound(Body body=null)
        {
            if (-2 == rounds[0])
                return;
            /*if (RshLib.Plugin.krokMpEnabled && RequestHostIfClient(0))
                return;*/
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
            if (Plugin.krokMpEnabled)
                SyncIfHost(2);
        }

        public int TotalRounds()
        {
            return rounds.Where(round => 0 <= round).Count();
        }

        public void Fill(sbyte type, int amount)
        {
            rounds = Enumerable.Repeat(type, amount).ToList();
            rounds.AddRange(Enumerable.Repeat((sbyte)-2, prop.capacity - amount));
            if (Plugin.krokMpEnabled && existed)
                SyncIfHost();
        }

        public void SyncIfHostt()
         => SyncIfHost(reliable: false);

        /*
         * 1 - Play load round
         * 2 - Play unload round
         */
        public void SyncIfHost(byte extraData=0, bool reliable=true)
        {
            if (!KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.is_server)
                return;
            var tracker = (GetMpTracker() as KrokoshaCasualtiesMP.KrokoshaScavMultiGameObjectNetworkTracker); // too much to type, duh
            if (!tracker.is_within_anyones_view)
                return;
            LiteNetLib.Utils.NetDataWriter writer = KrokoshaCasualtiesMP.Net.CreateWriter(31805);
            writer.Put((ushort)tracker.syncinfo.syncId);

            writer.Put((ushort)rounds.Count());
            for (ushort i = 0; i < rounds.Count(); i++)
                writer.Put((sbyte)rounds[i]);

            writer.Put((byte)extraData);

            LiteNetLib.DeliveryMethod method;
            if (reliable)
                method = LiteNetLib.DeliveryMethod.ReliableUnordered;
            else     method = LiteNetLib.DeliveryMethod.Unreliable;
            KrokoshaCasualtiesMP.Net.Server_SendToClients(method, in writer, KrokoshaCasualtiesMP.ServerMain.AllClientIdsExceptHost);
        }

        public bool RequestHostIfClient(byte action, Item second=null)
        {
            if (KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.is_server)
                return false;
            LiteNetLib.Utils.NetDataWriter writer = KrokoshaCasualtiesMP.Net.CreateWriter(31806);
            writer.Put((ushort)(GetMpTracker() as KrokoshaCasualtiesMP.KrokoshaScavMultiGameObjectNetworkTracker).syncinfo.syncId);
            writer.Put((byte)action);
            if (null != second)
            {
                if (!second.TryGetComponent<KrokoshaCasualtiesMP.KrokoshaScavMultiGameObjectNetworkTracker>(out var ksmgont2))
                    throw new Exception("[NewFirearms] Attempt to mag action on drag onto without KrokoshaScavMultiGameObjectNetworkTracker :hmm:");
                writer.Put((ushort)ksmgont2.syncinfo.syncId);
            }
            KrokoshaCasualtiesMP.Net.Client_Send(LiteNetLib.DeliveryMethod.ReliableUnordered, in writer);
            return true;
        }

        public static void ClientSync(KrokoshaCasualtiesMP.knetid _, ref LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            if (!KrokoshaCasualtiesMP.ItemSync.TryGetItem(new KrokoshaCasualtiesMP.knetid(syncId), out var _, out Item item))
                throw new Exception("[NewFirearms] Recived mag sync info for non-registred mag!");
            if (!item.TryGetComponent<RshMag>(out var rshMag))
                throw new Exception("[NewFirearms] Mag sync packet refering to item without RshMag component :tourniqet:");
            rshMag.existed = true;

            reader.Get(out ushort roundsCount);
            if (0 == roundsCount)
                throw new Exception("[NewFirearms] Attempt to initalize rounds with count of 0!");
/*            if (Plugin.sett.maxArraySize < roundsCount)
                throw new Exception($"[NewFirearms] Attempt to initalize array with count of {roundsCount}!");*/
            rshMag.rounds = new List<sbyte>(roundsCount); // do i need to delete old list?
            for (ushort i = 0; i < roundsCount; i++)
            {
                reader.Get(out sbyte newRound);
                rshMag.rounds.Add(newRound);
            }

            reader.Get(out byte extraData);
            if (1 == extraData)
                Sound.Play(rshMag.prop.loadRoundAudio, rshMag.transform.position);
            else if (2 == extraData)
                Sound.Play(rshMag.prop.unloadRoundAudio, rshMag.transform.position);
        }

        public static void ServerReceiver(KrokoshaCasualtiesMP.knetid clientId, ref LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            if (!KrokoshaCasualtiesMP.ItemSync.TryGetItem(new KrokoshaCasualtiesMP.knetid(syncId), out var si, out var item))
                throw new Exception("[NewFirearms] Recived mag action request for non-registred item!");
            if (!item.TryGetComponent<RshMag>(out var rshMag))
                throw new Exception("[NewFirearms] Mag action request refering to item without RshMag component :tourniqet:");
            if (!KrokoshaCasualtiesMP.NetPlayer.TryGetNetPlayerAndNetBodyFromClientId(clientId, out var plr, out var pb))
                throw new Exception("[NewFirearms] Recived mag action from client without body????!!!! WTF!!!!????");
            if (!KrokoshaCasualtiesMP.ItemSync.CheckIfBodyReachThisItem(si, pb.body))
                throw new Exception($"[NewFirearms] SUS: {plr} is trying to perfom action on mag they can not reach!");

            reader.Get(out byte action);
            if (0 == action)
                rshMag.RemoveRound(pb.body);
            else if (1 == action)
            {
                reader.Get(out ushort ontoSyncId);
                if (!KrokoshaCasualtiesMP.ItemSync.TryGetItem(new KrokoshaCasualtiesMP.knetid(ontoSyncId), out var ontoSi, out var ontoItem))
                    throw new Exception("[NewFirearms] Recived mag action drag onto for non-registred round!");
                if (!KrokoshaCasualtiesMP.ItemSync.CheckIfBodyReachThisItem(ontoSi, pb.body))
                    throw new Exception($"[NewFirearms] SUS: {plr} is trying to darg onto mag item they can not reach!");
                rshMag.DragOnto(ontoItem);
            }
            else     UnityEngine.Debug.LogWarning($"[NewFirearms] {plr} is trying to perform unknown mag action {action}");
        }
    }
}
