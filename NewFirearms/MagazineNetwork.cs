using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    internal static class MagazineNetwork
    {
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
            rshMag.UpdateSprite();
            if (1 == extraData)
                Sound.Play(rshMag.prop.loadRoundAudio, rshMag.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (2 == extraData)
                Sound.Play(rshMag.prop.unloadRoundAudio, rshMag.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
        }

        public static void ServerReceiver(Together.ScavPlayer plr, LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out byte action);

            if (2 <= action)
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.unknown"));
                return;
            }
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var si, out var item))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.unregistered", ModLocale.Get(RshMag.ACTIONS_NAME[action])));
                return;
            }
            if (!item.TryGetComponent<RshMag>(out var rshMag))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.invalid", ModLocale.Get(RshMag.ACTIONS_NAME[action])));
                return;
            }
            if (!plr.TryGetNetBody(out var pb))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.noBody", ModLocale.Get(RshMag.ACTIONS_NAME[action])));
                return;
            }
            if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(si, pb.body, check_obstruction:true))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.outOfReach", ModLocale.Get(RshMag.ACTIONS_NAME[action])), canBeFixedWithSwitchingStrictSync:true);
                return;
            }

            if (0 == action)
                rshMag.RemoveRound(pb.body);
       else if (1 == action)
            {
                reader.Get(out ushort ontoSyncId);
                if (!Together.ItemSync.TryGetItem(new Together.knetid(ontoSyncId), out var ontoSi, out var ontoItem))
                {
                    NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.roundUnregistered"));
                    return;
                }
                if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(ontoSi, pb.body, check_obstruction:true))
                {
                    NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.mag.roundOutOfReach"), canBeFixedWithSwitchingStrictSync:true);
                    return;
                }
                rshMag.DragOnto(ontoItem);
            }
        }
    }
}
