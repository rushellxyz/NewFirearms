using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    // Kept off saveable components: Mono cannot reflect signatures with missing optional assemblies.
    internal static class GunNetwork
    {
        public static void ReceiveEjectionVisual(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out sbyte round);
            reader.Get(out sbyte ammoIndex);
            if (Together.ItemSync.TryGetItem(new Together.knetid(syncId), out _, out var item) &&
                item != null && item.TryGetComponent<RshGun>(out var gun) && gun.prop != null)
                gun.ShowEjectionVisual(round, ammoIndex);
        }

        public static void ClientSync(LiteNetLib.Utils.NetDataReader reader)
        { // the var is evil
            reader.Get(out ushort syncId);
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var _, out var it))
                throw new Exception("[NewFirearms] Recived gun sync info for non-registred gun!");
            if (!it.TryGetComponent<RshGun>(out var rshGun))
                throw new Exception("[NewFirearms] Gun sync packet refering to item without RshGun component :tourniqet:");
            rshGun.existed = true;
            bool shouldUpdateSprite = false;

            reader.Get(out byte newFireMode);
            if (newFireMode != rshGun.fireMode)
                Sound.Play(rshGun.prop.toggleFireModeAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            rshGun.fireMode = newFireMode;

            reader.Get(out bool newRack);

            string newMag = null;
            reader.Get(out bool haveMag);
            if (haveMag)
                Together.MyLiteNetLibExtensions.Get(reader, out newMag, oneByteChars: true);
            bool magazineRemoved = string.IsNullOrEmpty(newMag) && !string.IsNullOrEmpty(rshGun.mag);
            if (magazineRemoved)
            {
                shouldUpdateSprite = true;
            }
            else if (!string.IsNullOrEmpty(newMag) && string.IsNullOrEmpty(rshGun.mag))
            {
                shouldUpdateSprite = true;
                Sound.Play(rshGun.prop.loadMagAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
            rshGun.mag = newMag;

            reader.Get(out ushort roundsCount);
            if (0 == roundsCount)
                throw new Exception("[NewFirearms] Attempt to initalize rounds with count of 0!");
            if (rshGun.rounds == null) rshGun.rounds = new List<sbyte>(roundsCount);
            else rshGun.rounds.Clear();
            for (ushort i = 0; i < roundsCount; i++)
            {
                reader.Get(out sbyte newRound);
                rshGun.rounds.Add(newRound);
            }

            reader.Get(out byte extraData);
            ReadJamState(rshGun, reader);
            bool silentMagazineRemoval = (extraData & RshGun.SilentMagazineRemoval) != 0;
            extraData = (byte)(extraData & ~RshGun.SilentMagazineRemoval);
            if (!silentMagazineRemoval && (magazineRemoved || 1 == extraData))
                Sound.Play(rshGun.prop.removeMagAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            if (2 == extraData)
                Sound.Play(rshGun.prop.unshootAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (3 == extraData)
                Sound.Play(rshGun.prop.jamAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (4 == extraData)
                Sound.Play(rshGun.prop.loadRoundAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));

            if (newRack && !rshGun.racked)
            {
                shouldUpdateSprite = true;
                if (5 == extraData)
                    Sound.Play(rshGun.prop.rackAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
       else if (!newRack && rshGun.racked)
            {
                shouldUpdateSprite = true;
                if (5 == extraData)
                    Sound.Play(rshGun.prop.unrackAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
            rshGun.racked = newRack;

            if (shouldUpdateSprite)
                rshGun.UpdateSprite();
        }

        public static void ServerReceiver(Together.ScavPlayer plr, LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out byte action);

            if (7 <= action)
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.unknown"));
                return;
            }
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var si, out Item it))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.unregistered", ModLocale.Get(RshGun.ACTIONS_NAME[action])));
                return;
            }
            if (!it.TryGetComponent<RshGun>(out var rshGun))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.invalid", ModLocale.Get(RshGun.ACTIONS_NAME[action])));
                return;
            }
            if (!plr.TryGetNetBody(out var pb))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.noBody", ModLocale.Get(RshGun.ACTIONS_NAME[action])));
                return;
            }
            if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(si, pb.body, check_obstruction:true))
            {
                NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.outOfReach", ModLocale.Get(RshGun.ACTIONS_NAME[action])), canBeFixedWithSwitchingStrictSync:true);
                return;
            }

            if (0 == action)
                rshGun.Shoot(pb.body);
       else if (1 == action)
                rshGun.Rack(manual: true);
       else if (2 == action)
            {
                reader.Get(out ushort ontoSyncId);
                if (!Together.ItemSync.TryGetItem(new Together.knetid(ontoSyncId), out var ontoSi, out var ontoIt))
                {
                    NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.roundUnregistered"));
                    return;
                }
                if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(ontoSi, pb.body, check_obstruction:true))
                {
                    NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.roundOutOfReach"), canBeFixedWithSwitchingStrictSync:true);
                    return;
                }
                rshGun.DragOnto(ontoIt);
            }
       else if (3 == action)
                rshGun.RemoveMag(pb.body);
       else if (4 == action)
                rshGun.ToggleFireMode();
       else if (5 == action)
                rshGun.ReleaseTrigger();
       else if (6 == action)
                rshGun.ClearJam();
        }

        public static void ReceiveShoot(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            RshGun rshGun = null;
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var si, out var _1))
                UnityEngine.Debug.LogWarning("[NewFirearms] Recived shoot notify for non-registred item! Are you missing addon?");
       else if (!si.IsItem())
                UnityEngine.Debug.LogWarning("[NewFirearms] Shoot notify refering to not item :tourniqet:");
       else if (!si.go.TryGetComponent<RshGun>(out rshGun))
                UnityEngine.Debug.LogWarning("[NewFirearms] Shoot notify refering to item without RshGun component :tourniqet:");

            reader.Get(out float knockback);
            reader.Get(out float muzzleRise);
            reader.Get(out bool ragdollRecoil);
            if (null != rshGun)
                Sound.Play(rshGun.prop.shootAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(si.item), pitchShift: false);
            if (null != rshGun && null != rshGun.transform.parent && rshGun.transform.parent.TryGetComponent<InventorySlot>(out InventorySlot slot))
            {
                float num = slot.body.isRight ? 1f : (-1f);
                slot.body.rb.velocity -= (Vector2)rshGun.transform.right * num * knockback;
                slot.body.lastTimeStepVelocity -= (Vector2)rshGun.transform.right * num * knockback;
                slot.body.armsAnimator.SetFloat("gunangle", slot.body.armsAnimator.GetFloat("gunangle") + muzzleRise * RshGun.MUZZLE_RISE_MODIFIER);
                if (PlayerCamera.main.body == slot.body && GunMinigame.Plugin.useMinigame)
                    GunMinigame.MinigameManager.GetOrAddInstance().AddRecoil(knockback, muzzleRise);
                if (ragdollRecoil)
                    slot.body.Ragdoll();
            }
       else     UnityEngine.Debug.LogWarning("[NewFirearms] Host says that gun is being shot, but im unable to find who shoots it, ummm, okay ig???");
            ShootManager.DrawVisuals(ShootVisuals.Deserialize(ref reader));
        }

        internal static void WriteJamState(RshGun gun, LiteNetLib.Utils.NetDataWriter writer)
        {
            gun.GetJamState(out byte kind, out ushort chamber, out sbyte ammo);
            writer.Put(kind);
            writer.Put(chamber);
            writer.Put(ammo);
        }

        private static void ReadJamState(RshGun gun, LiteNetLib.Utils.NetDataReader reader)
        {
            // Older gun snapshots end after extraData.
            if (reader.AvailableBytes < 4)
            {
                gun.ApplyJamState(0, 0, -1);
                return;
            }
            reader.Get(out byte kind);
            reader.Get(out ushort chamber);
            reader.Get(out sbyte ammo);
            gun.ApplyJamState(kind, chamber, ammo);
        }
    }
}
