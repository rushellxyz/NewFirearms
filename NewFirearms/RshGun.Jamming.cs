using Newtonsoft.Json;
using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        private enum GunJam : byte { None, Ejection, Feed }

        [JsonProperty("jam")]
        private GunJam jam;
        [JsonProperty("jamChamber")]
        private ushort jamChamber;
        [JsonProperty("jamAmmo")]
        private sbyte jamAmmo = -1;

        public bool IsJammed => jam != GunJam.None;

        private void BeginJam(GunJam kind, int chamber, sbyte ammo)
        {
            jam = kind;
            jamChamber = (ushort)chamber;
            jamAmmo = ammo;
            racked = true;
            gasTime = 0f;
            shooter = null;
            currentlyShooting = false;
            Sound.Play(prop.jamAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            UpdateSprite();
            if (Plugin.togetherMpEnabled) SyncIfHost(3);
        }

        public bool TryGetJam(out Sprite sprite, out bool liveRound)
        {
            liveRound = jam == GunJam.Feed;
            sprite = null;
            if (!IsJammed || prop == null) return false;
            if (jamAmmo >= 0 && jamAmmo < prop.ammoTypes.Count)
            {
                if (liveRound && prop.minigame?.casingsSprite != null && jamAmmo + 1 < prop.minigame.casingsSprite.Length)
                    sprite = prop.minigame.casingsSprite[jamAmmo + 1];
                else if (!liveRound)
                    sprite = prop.ammoTypes[jamAmmo].loadedMinigameCasingSprite;
                if (liveRound && sprite == null && prop.minigame?.ammosInHand != null &&
                    jamAmmo < prop.minigame.ammosInHand.Length)
                    sprite = prop.minigame.ammosInHand[jamAmmo];
            }
            if (sprite == null && prop.minigame?.casingsSprite != null && prop.minigame.casingsSprite.Length > 0)
                sprite = prop.minigame.casingsSprite[0];
            return true;
        }

        public void ClearJam()
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(6)) return;
            if (!IsJammed) return;
            var previousJam = jam;
            int chamber = jamChamber;
            jam = GunJam.None;
            jamAmmo = -1;
            gasTime = 0f;
            shooter = null;
            currentlyShooting = false;
            // Only remove the obstruction; duplicate requests cannot eject the next round.
            if (chamber < rounds.Count &&
                (previousJam == GunJam.Ejection ? rounds[chamber] == -1 : rounds[chamber] >= 0))
                EjectRound(chamber, allowJam: false);
            racked = true;
            Sound.Play(prop.rackAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            UpdateSprite();
            if (Plugin.togetherMpEnabled) SyncIfHost(5);
        }

        private void WriteJamState(LiteNetLib.Utils.NetDataWriter writer)
        {
            writer.Put((byte)jam);
            writer.Put(jamChamber);
            writer.Put(jamAmmo);
        }

        private void ReadJamState(LiteNetLib.Utils.NetDataReader reader)
        {
            /*// Older gun snapshots end after extraData.
            if (reader.AvailableBytes < 4)
            {
                jam = GunJam.None;
                return;
            }*/
            reader.Get(out byte kind);
            reader.Get(out ushort chamber);
            reader.Get(out sbyte ammo);
            jam = kind <= (byte)GunJam.Feed ? (GunJam)kind : GunJam.None;
            jamChamber = chamber;
            jamAmmo = ammo;
        }
    }
}
