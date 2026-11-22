using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    // Kept off saveable components: Mono cannot reflect signatures with missing optional assemblies.
    internal static class GunNetwork
    {
        internal const string LoadHandlingMessage = "NewFirearms_GunLoadHandling";

        internal static void SendLoadHandling(RshGun gun, Body actor, bool magazine)
        {
            if (!Together.ScavPlayer.BodyToPlayerDict.ContainsKey(actor) ||
                !(gun.GetMpTracker(false) is Together.SyncInfoGameObjectTracker tracker))
                return;

            var writer = Together.Multiplayer.CreateNamedWriter(LoadHandlingMessage);
            writer.Put((ushort)tracker.syncId);
            writer.Put(magazine);
            var clientId = Together.ScavPlayer.GetClientIdFromBody(actor);
            Together.Net.Server_SendToClients(LiteNetLib.DeliveryMethod.ReliableOrdered, in writer, clientId);
        }

        internal static void ReceiveLoadHandling(LiteNetLib.Utils.NetDataReader reader)
        {
            if (reader.AvailableBytes < 3) return;
            var syncId = new Together.knetid(reader.GetUShort());
            bool magazine = reader.GetBool();
            if (Together.ItemSync.TryGetItem(syncId, out _, out var item) &&
                item != null && item.TryGetComponent<RshGun>(out var gun))
                gun.PlayLoadHandlingImpulse(magazine);
        }

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
        {
            reader.Get(out ushort syncId);
            // Snapshots can race item creation or target a gun represented by GunScript on this client.
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out _, out var it) ||
                it == null || !it.TryGetComponent<RshGun>(out var rshGun) || rshGun.prop == null)
                return;
            if (!rshGun.AcceptSnapshot(reader.GetUInt())) return;
            rshGun.existed = true;
            bool shouldUpdateSprite = false;

            reader.Get(out byte newFireMode);
            if (newFireMode != rshGun.fireMode)
                Sound.Play(rshGun.prop.toggleFireModeAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            rshGun.fireMode = newFireMode;

            reader.Get(out bool newRack);
            if (rshGun.prop.feedType == FeedKind.Battery) newRack = false;

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
            rshGun.SetBatteryState(BatteryNetwork.Read(reader));
            ReadTetherState(rshGun, reader);
            rshGun.FlashlightEnabled = reader.GetBool() && rshGun.HasFlashlight();
            // не они не omit т
            // Older snapshots omit the chamber's spent-ammo type.
            rshGun.ChamberSpentAmmo = /*reader.AvailableBytes > 0 ?*/ reader.GetSByte() /*: (sbyte)-1*/;
            /*// Older snapshots predate the laser flag and keep the saved/default state.
            if (reader.AvailableBytes > 0)*/ rshGun.ApplyLaserState(reader.GetBool());
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
       else if (6 == extraData)
                Sound.Play(rshGun.prop.rackAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (7 == extraData)
                Sound.Play("flashlighttoggle", rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));

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
            rshGun.ObserveRackSnapshot(newRack);

            if (shouldUpdateSprite)
                rshGun.UpdateSprite();
        }

        public static void ServerReceiver(Together.ScavPlayer plr, LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out byte action);

            if (10 <= action)
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

            if (0 == action || 8 == action)
            {
                // надеюсь эта проверка ничего не сломала
                if (pb.body.GetItem(0) != it && pb.body.GetItem(1) != it) return;
                Vector2? clientGunPosition = null;
                // Older clients send only the gun ID and action; use the host position for those.
                if (reader.AvailableBytes >= 8)
                {
                    Vector2 position = new Vector2(reader.GetFloat(), reader.GetFloat());
                    if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
                        float.IsNaN(position.y) || float.IsInfinity(position.y))
                    {
                        NetworkAction.DenyAction(plr, ModLocale.Get("newfirearms.denied.gun.invalidShotPosition"));
                        return;
                    }
                    if (!Plugin.sett.strictSync) clientGunPosition = position;
                }
                bool halfRecoil = reader.AvailableBytes > 0 && reader.GetBool();
                if (action == 0) rshGun.Shoot(pb.body, clientGunPosition, halfRecoil);
                else rshGun.UpdateClientShotState(pb.body, clientGunPosition, halfRecoil);
            }
       else if (1 == action)
            {
                // Old clients omit the target and retain the original toggle behavior.
                bool? target = reader.AvailableBytes > 0 ? (bool?)reader.GetBool() : null;
                if (rshGun.prop.feedType != FeedKind.Battery && target.HasValue && rshGun.racked == target.Value)
                    rshGun.SyncIfHost();
                else
                    rshGun.Rack(manual: true, actor: pb.body);
            }
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
                rshGun.DragOnto(ontoIt, pb.body);
            }
       else if (3 == action)
                rshGun.RemoveMag(pb.body);
       else if (4 == action)
                rshGun.ToggleFireMode();
       else if (5 == action)
                rshGun.ReleaseTrigger();
       else if (6 == action)
                rshGun.ClearJam();
       else if (7 == action)
                rshGun.ToggleFlashlight();
       else if (9 == action && reader.AvailableBytes > 0)
                rshGun.SetLaserEnabled(reader.GetBool());
        }

        public static void ReceiveShoot(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out float knockback);
            reader.Get(out float muzzleRise);
            reader.Get(out bool ragdollRecoil);
            if (Together.ItemSync.TryGetItem(new Together.knetid(syncId), out _, out var item) &&
                item != null && item.TryGetComponent<RshGun>(out var gun) && gun.prop != null)
            {
                Sound.Play(gun.prop.shootAudio, gun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(item), pitchShift: false);
                if (gun.transform.parent != null && gun.transform.parent.TryGetComponent<InventorySlot>(out var slot))
                {
                    NativeGameFeatures.UnlockFiringAchievement(slot.body);
                    float direction = slot.body.isRight ? 1f : -1f;
                    slot.body.rb.velocity -= (Vector2)gun.transform.right * direction * knockback;
                    slot.body.lastTimeStepVelocity -= (Vector2)gun.transform.right * direction * knockback;
                    slot.body.armsAnimator.SetFloat("gunangle", slot.body.armsAnimator.GetFloat("gunangle") + muzzleRise * RshGun.MUZZLE_RISE_MODIFIER);
                    if (PlayerCamera.main.body == slot.body && GunMinigame.Plugin.useMinigame)
                        GunMinigame.MinigameManager.GetOrAddInstance().AddRecoil(knockback, muzzleRise);
                    if (ragdollRecoil)
                        slot.body.Ragdoll();
                }
            }
            ShootManager.DrawVisuals(ShootVisuals.Deserialize(ref reader));
        }

        internal static void WriteTetherState(RshGun gun, LiteNetLib.Utils.NetDataWriter writer)
        {
            writer.Put(gun.needsRetraction);
            if (!gun.needsRetraction) return;
            Vector2 point = gun.TetherPosition;
            writer.Put(point.x);
            writer.Put(point.y);
            var tracker = gun.TetherTarget != null
                ? gun.TetherTarget.GetComponentInParent<Together.SyncInfoGameObjectTracker>() : null;
            writer.Put(tracker != null);
            if (tracker == null) return;
            writer.Put((ushort)tracker.syncId);
            Vector2 local = tracker.transform.InverseTransformPoint(point);
            writer.Put(local.x);
            writer.Put(local.y);
        }

        private static void ReadTetherState(RshGun gun, LiteNetLib.Utils.NetDataReader reader)
        {
            bool active = reader.GetBool();
            if (!active)
            {
                gun.ApplyTetherState(false, Vector2.zero);
                return;
            }
            Vector2 point = new Vector2(reader.GetFloat(), reader.GetFloat());
            Transform target = null;
            if (reader.GetBool())
            {
                ushort id = reader.GetUShort();
                Vector2 local = new Vector2(reader.GetFloat(), reader.GetFloat());
                if (Together.NetObjectRegistry.TryGetSyncInfo(new Together.knetid(id), out var info) && info.go != null)
                {
                    target = info.go.transform;
                    point = target.TransformPoint(local);
                }
            }
            gun.ApplyTetherState(true, point, target);
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
            reader.Get(out byte kind);
            reader.Get(out ushort chamber);
            reader.Get(out sbyte ammo);
            gun.ApplyJamState(kind, chamber, ammo);
        }
    }
}
