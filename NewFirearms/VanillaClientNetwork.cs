using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LiteNetLib.Utils;
using Together;
using UnityEngine;

namespace NewFirearms
{
    // Only reached behind the optional multiplayer capability check.
    internal static class VanillaClientNetwork
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool CanInstall()
        {
            string[] required = {
                "Together.GunScript_Fire_MultiplayerPatch:ServerReceiver__PlayerBodyShoot",
                "Together.GunScript_Fire_MultiplayerPatch:Client_PlayerBodyShoot_Relay",
                "Together.CombatStuff:ServerReceiver__GunRack",
                "Together.CombatStuff:ServerReceiver__GunEjectMag",
                "Together.CombatStuff:ServerReceiver__GunToggleSafety",
                "Together.PlayerCamera_GunRack_MultiplayerPatch:Postfix",
                "Together.PlayerCamera_GunEjectMag_MultiplayerPatch:Postfix",
                "Together.PlayerCamera_GunToggleSafety_MultiplayerPatch:Postfix"
            };
            foreach (string method in required)
            {
                var target = AccessTools.Method(method);
                var parameters = target?.GetParameters();
                bool cameraPostfix = method.EndsWith(":Postfix", StringComparison.Ordinal);
                bool clientRelay = method.EndsWith(":Client_PlayerBodyShoot_Relay", StringComparison.Ordinal);
                if (target != null && target.IsStatic &&
                    (cameraPostfix
                        ? parameters.Length == 1 && parameters[0].ParameterType == typeof(PlayerCamera)
                        : parameters.Length == 2 && parameters[0].ParameterType == typeof(knetid) &&
                          (clientRelay || parameters[0].Name == "clientId") && parameters[1].ParameterType == typeof(NetDataReader) &&
                          parameters[1].Name == "reader")) continue;
                Debug.LogWarning($"[NewFirearms] Native-client compatibility disabled: {method} is unavailable. Modded clients remain supported.");
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Install(Harmony harmony)
        {
            if (!Plugin.nativeClientCompatibilityAvailable) return;
            Patch(harmony, "Together.GunScript_Fire_MultiplayerPatch", "ServerReceiver__PlayerBodyShoot", nameof(ReceiveShot));
            Patch(harmony, "Together.GunScript_Fire_MultiplayerPatch", "Client_PlayerBodyShoot_Relay", nameof(SkipNativeShotRelay));
            Patch(harmony, "Together.CombatStuff", "ServerReceiver__GunRack", nameof(ReceiveRack));
            Patch(harmony, "Together.CombatStuff", "ServerReceiver__GunEjectMag", nameof(ReceiveEject));
            Patch(harmony, "Together.CombatStuff", "ServerReceiver__GunToggleSafety", nameof(ReceiveSafety));
            foreach (string type in new[] { "PlayerCamera_GunRack_MultiplayerPatch", "PlayerCamera_GunEjectMag_MultiplayerPatch",
                "PlayerCamera_GunToggleSafety_MultiplayerPatch" })
                Patch(harmony, "Together." + type, "Postfix", nameof(SkipNativeRequest));
            // The proxy must not disable Catpatch's pre-existing RshGun adapter.
            var selfHarmGuard = AccessTools.Method("CUCoreLib.Patches.ItemRegistryPatches:PreventCustomGunSelfHarm");
            if (Plugin.catPatchActive && selfHarmGuard != null)
                harmony.Patch(selfHarmGuard, prefix: new HarmonyMethod(typeof(VanillaClientNetwork), nameof(KeepModdedGunAdapter)));
        }

        private static void Patch(Harmony harmony, string type, string method, string prefix)
        {
            var target = AccessTools.Method(type + ":" + method);
            if (target == null)
            {
                Debug.LogWarning($"[NewFirearms] Native-client compatibility: {type}.{method} is unavailable.");
                return;
            }
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(VanillaClientNetwork), prefix));
        }

        // These targets are static Together patch methods; __0 is their camera argument.
        private static bool SkipNativeRequest(PlayerCamera __0)
            => __0 == null || __0.body == null || __0.body.GetItem(__0.body.handSlot)?.GetComponent<RshGun>() == null;

        private static bool KeepModdedGunAdapter(GunScript __0, bool __1, ref bool __result)
        {
            if (!__1 || __0 == null || __0.GetComponent<RshGun>() == null) return true;
            __result = true;
            return false;
        }

        private static bool SkipNativeShotRelay(NetDataReader __1)
        {
            if (__1.AvailableBytes < 4) return true;
            int position = __1.Position;
            __1.Get(out ushort shooterId);
            __1.Get(out ushort syncId);
            __1.SetPosition(position);
            // Custom visuals already include recoil and casings. Do not also run
            // Together's native relay on a peer that has the authoritative adapter.
            return !ItemSync.TryGetItem(new knetid(syncId), out _, out var item) || item == null ||
                item.GetComponent<RshGun>() == null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void PublishState(GameObject item)
        {
            if (item.GetComponent<SyncInfoGameObjectTracker>() != null)
                NetObjectRegistry.SafeNewGO(item);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void RelayShot(RshGun gun, Body body, Vector2 origin)
        {
            if (!Plugin.nativeClientCompatibilityAvailable) return;
            if (body == null || !ScavPlayer.BodyToPlayerDict.ContainsKey(body)) return;
            var tracker = gun.GetMpTracker(false) as SyncInfoGameObjectTracker;
            if (tracker == null) return;
            knetid shooterId = ScavPlayer.GetClientIdFromBody(body);
            var writer = Net.CreateWriter(NetmsgId.CLIENT_PlayerBodyShoot_Relay);
            writer.Put((ushort)shooterId);
            writer.Put((ushort)tracker.syncId);
            writer.Put(origin);
            writer.Put((Vector2)gun.transform.right * (body.isRight ? 1f : -1f));
            Net.Server_SendToClients(LiteNetLib.DeliveryMethod.ReliableUnordered, in writer,
                ServerMain.GetListOfClientIdsExceptThisAndHost(shooterId));
        }

        private static bool TryGetGun(knetid clientId, out Body body, out RshGun gun)
        {
            body = null;
            gun = null;
            if (!ScavPlayer.TryGetPlayerAndNetBodyFromClientId(clientId, out _, out var netBody) || netBody.body == null)
                return false;
            body = netBody.body;
            gun = body.GetItem(body.handSlot)?.GetComponent<RshGun>();
            return gun != null && gun.prop != null;
        }

        private static bool ReceiveShot(knetid clientId, NetDataReader reader)
        {
            if (!Plugin.IsHost() || reader.AvailableBytes < 4) return true;
            int position = reader.Position;
            reader.Get(out ushort tick);
            reader.Get(out ushort syncId);
            if (!ItemSync.TryGetItem(new knetid(syncId), out _, out var item) || item == null ||
                !item.TryGetComponent<RshGun>(out var gun))
            {
                reader.SetPosition(position);
                return true;
            }
            // Ignore the native hit claims. The host traces and damages using RshGun's ammunition.
            if (reader.AvailableBytes < 17 || !TryGetGun(clientId, out var body, out var heldGun) ||
                gun != heldGun || !body.conscious) return false;
            MyLiteNetLibExtensions.Get(reader, out Vector2 origin);
            MyLiteNetLibExtensions.Get(reader, out Vector2 direction);
            reader.Get(out bool racked);
            if (!Finite(origin) || !Finite(direction) || direction.sqrMagnitude < 0.001f ||
                Vector2.Distance(origin, gun.transform.position) > 10f) return false;
            gun.RecordNativeRackEcho(body);
            gun.ShootNativeRound(body);
            return false;
        }

        private static bool ReceiveRack(knetid clientId, NetDataReader reader)
        {
            if (!Plugin.IsHost() || !TryGetGun(clientId, out var body, out var gun)) return true;
            if (!body.conscious || reader.AvailableBytes < 1) return false;
            bool target = reader.GetBool();
            /*
            Handle rack acknowledgements arriving before shots
            In ReceiveRack, the next rack packet is discarded unconditionally when an echo is pending. Together sends shots and rack acknowledgements with ReliableUnordered delivery, so an acknowledgement can arrive first and leave no echo to consume. ReceiveShot then records a stale echo, causing the player's next manual rack within two seconds to be ignored. Make acknowledgement tracking tolerate reordered delivery instead of consuming the next rack request blindly.
            */
            if (gun.ConsumeNativeRackEcho(body)) return false;
            if (gun.IsJammed)
            {
                if (!target) gun.ClearJam();
            }
            else if (gun.needsRetraction)
            {
                if (!target) gun.Rack(manual: true, actor: body);
            }
            else if (gun.racked != target && (target || gun.gasTime <= 0f))
                gun.Rack(manual: true, actor: body);
            gun.SyncIfHost();
            return false;
        }

        private static bool ReceiveEject(knetid clientId, NetDataReader reader)
        {
            if (!Plugin.IsHost() || !TryGetGun(clientId, out var body, out var gun)) return true;
            if (body.conscious) gun.RemoveMag(body);
            return false;
        }

        private static bool ReceiveSafety(knetid clientId, NetDataReader reader)
        {
            if (!Plugin.IsHost() || !TryGetGun(clientId, out var body, out var gun)) return true;
            if (!body.conscious || reader.AvailableBytes < 1) return false;
            bool safe = reader.GetBool();
            gun.ReleaseTrigger();
            gun.fireMode = safe ? (byte)0 : System.Linq.Enumerable.Max(gun.prop.fireModes);
            gun.UpdateSprite();
            gun.SyncIfHost();
            return false;
        }

        private static bool Finite(Vector2 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }
}
