using System.Linq;
using CUCoreLib.Data;
using HarmonyLib;
using UnityEngine;

namespace NewFirearms
{
    // CUCoreLib can send native gun definitions to peers without NewFirearms.
    // RshGun remains authoritative; GunScript only exposes the native protocol/UI state.
    internal sealed class VanillaGunProxy : MonoBehaviour
    {
        private RshGun gun;
        private GunScript native;

        internal static GunProperties CreateDefinition(GunJson definition, Sprite normal)
        {
            AmmoInfo ammo = definition.ammoTypes[0];
            Sprite racked = ItemLoader.TextureToSprite(definition.rackedTexture, definition.ppu);
            Texture2D magazine = definition.magazineTextures?.Values.FirstOrDefault();
            Sprite normalWithMag = magazine == null ? normal : ItemLoader.TextureToSprite(
                Plugin.CombineTextures(new System.Collections.Generic.List<Texture2D> { definition.normalTexture, magazine }), definition.ppu);
            Sprite rackedWithMag = magazine == null ? racked : ItemLoader.TextureToSprite(
                Plugin.CombineTextures(new System.Collections.Generic.List<Texture2D> { definition.rackedTexture, magazine }), definition.ppu);
            int capacity = definition.feedType == FeedKind.Magazine || definition.feedType == FeedKind.Battery
                ? 30 : Mathf.Max(0, definition.internalCapacity - 1);
            return new GunProperties
            {
                AmmoType = GetAmmoType(ammo.round),
                FiringMode = GetFiringMode(definition.fireModes.Max()),
                FeedType = definition.feedType == FeedKind.Magazine || definition.feedType == FeedKind.Battery
                    ? GunScript.FeedType.Mag : GunScript.FeedType.Direct,
                MagCapacity = capacity,
                KnockBack = ammo.knockback,
                StructureDamage = ammo.structureDamage,
                AnimalDamage = ammo.animalDamage,
                Loudness = ammo.loudness,
                DesiredGasTime = Mathf.Max(0.05f, definition.desiredGasTime),
                ShotsPerFire = Mathf.Max(1, ammo.pellets),
                VerticalSpread = ammo.verticalSpread,
                ConditionLossPerShot = ammo.conditionLoss,
                FireSound = definition.shootAudio,
                CustomRack = definition.rackAudio,
                CustomUnrack = definition.unrackAudio,
                NormalSprite = normalWithMag,
                RackedSprite = rackedWithMag,
                NormalSpriteNoMag = normal,
                RackedSpriteNoMag = racked,
                BarrelOffset = new Vector2(0f, definition.barrelOffset)
            };
        }

        internal static GunScript.AmmoType GetAmmoType(string round)
            => round == "12gauge" || round.StartsWith("nf.12gauge") ? GunScript.AmmoType.Shotgun
                : round == "556round" || round.StartsWith("nf.556") ? GunScript.AmmoType.Rifle
                : GunScript.AmmoType.Pistol;

        internal static void FinalizeDefinitions()
        {
            foreach (var pair in Plugin.GunDefinitions)
            {
                if (!CUCoreLib.Registries.ItemRegistry.TryGetCustomInfo(pair.Key, out var item) || item.Gun == null) continue;
                var definition = pair.Value;
                if (definition.feedType != FeedKind.Magazine && definition.feedType != FeedKind.Battery) continue;
                int capacity = Plugin.MagazineDefinitions.Values
                    .Where(mag => definition.magazineType != null && mag.magazineType != null &&
                        definition.magazineType.Intersect(mag.magazineType).Any())
                    .Select(mag => mag.capacity).DefaultIfEmpty(0).Max();
                item.Gun.MagCapacity = definition.feedType == FeedKind.Battery
                    ? Mathf.FloorToInt(capacity / definition.batteryDrainPerShot) : capacity;
            }
        }

        private static GunScript.FiringMode GetFiringMode(byte mode)
            => mode == 3 ? GunScript.FiringMode.Auto
                : mode == 1 ? GunScript.FiringMode.Pump : GunScript.FiringMode.SemiAuto;

        internal static void Attach(RshGun gun)
        {
            // CUCoreLib's native gun template is a pistol, including its sight.
            // Keep the transform hierarchy intact; our configurable laser is a
            // separate LaserPointer/LineRenderer on the gun itself.
            Transform sight = gun.transform.Find("Laser");
            if (sight != null && sight.TryGetComponent<SpriteRenderer>(out var renderer) &&
                renderer.sprite != null && renderer.sprite.name == "laserpointer")
                sight.gameObject.SetActive(false);

            if (!Plugin.nativeClientCompatibilityAvailable) return;
            var proxy = gun.GetComponent<VanillaGunProxy>() ?? gun.gameObject.AddComponent<VanillaGunProxy>();
            proxy.gun = gun;
            proxy.native = gun.GetComponent<GunScript>() ?? gun.gameObject.AddComponent<GunScript>();
            proxy.native.enabled = false;
            if (proxy.native.barrel == null)
            {
                var barrel = new GameObject("VanillaClientBarrel");
                barrel.transform.SetParent(gun.transform, false);
                barrel.transform.localPosition = new Vector3(0f, gun.prop.barrelOffset);
                proxy.native.barrel = barrel.transform;
            }
            proxy.Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (gun == null || native == null || gun.prop == null || gun.rounds == null || gun.rounds.Count == 0) return;
            bool racked = gun.racked || gun.IsJammed || gun.needsRetraction;
            bool hasMag = !string.IsNullOrEmpty(gun.mag);
            int rounds = gun.rounds.Skip(1).Count(round => round >= 0);
            var chamber = gun.rounds[0] >= 0 ? GunScript.RoundInChamber.Round
                : gun.rounds[0] == -1 ? GunScript.RoundInChamber.Casing : GunScript.RoundInChamber.None;
            if (gun.UsesOpenBolt && !gun.IsJammed)
            {
                // The native client requests shots from a closed bolt with a chambered round.
                racked = !gun.racked;
                chamber = GunScript.RoundInChamber.None;
                if (gun.racked && (gun.rounds[0] >= 0 || hasMag && rounds > 0))
                {
                    chamber = GunScript.RoundInChamber.Round;
                    if (gun.rounds[0] < 0) rounds--;
                }
            }
            else if (gun.prop.feedType == FeedKind.Battery)
            {
                int shots = Mathf.FloorToInt((BatteryCells.Charge(gun.magazineBatteries) + 0.000001f) / gun.prop.batteryDrainPerShot);
                chamber = shots > 0 ? GunScript.RoundInChamber.Round : GunScript.RoundInChamber.None;
                rounds = Mathf.Max(0, shots - 1);
            }
            bool safe = gun.fireMode == 0;
            bool changed = native.racked != racked || native.hasMag != hasMag || native.roundsInMag != rounds ||
                native.roundInChamber != chamber || native.safe != safe;
            native.enabled = false;
            native.ammoType = GetAmmoType(gun.prop.ammoTypes[0].round);
            native.firingMode = GetFiringMode(gun.fireMode);
            native.feedType = gun.prop.feedType == FeedKind.Magazine || gun.prop.feedType == FeedKind.Battery
                ? GunScript.FeedType.Mag : GunScript.FeedType.Direct;
            native.magCapacity = !string.IsNullOrEmpty(gun.mag) && Plugin.MagazineDefinitions.TryGetValue(gun.mag, out var mag)
                ? mag.capacity : Mathf.Max(0, gun.prop.internalCapacity - 1);
            native.racked = native.lastRacked = racked;
            native.hasMag = hasMag;
            native.roundsInMag = rounds;
            native.roundInChamber = chamber;
            native.safe = safe;
            native.triggerPressed = false;
            if (changed && Plugin.MultiplayerActive && Plugin.IsHost())
                VanillaClientNetwork.PublishState(gameObject);
        }

        [HarmonyPatch(typeof(GunScript), "Update")]
        private static class DisableNativeSimulation
        {
            private static bool Prefix(GunScript __instance) => __instance.GetComponent<RshGun>() == null;
        }
    }
}
