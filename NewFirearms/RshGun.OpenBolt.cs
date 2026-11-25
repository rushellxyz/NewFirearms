using Newtonsoft.Json;
using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        [JsonProperty("openBoltCycling")]
        public bool openBoltCycling;

        public bool UsesOpenBolt => prop?.openBolt == true;

        private float OpenBoltCycleTime => Mathf.Max(0.05f, prop.desiredGasTime);

        private bool CanReleaseOpenBolt()
            => racked && !openBoltCycling && !IsJammed && fireMode != 0 && rounds.Count > 0 && it.condition > 0.005f;

        private int FindOpenBoltRound()
        {
            // An older save can already have a chambered round.
            if (rounds[0] >= 0) return 0;
            if (!string.IsNullOrEmpty(mag) && rounds.Count > 1 && rounds[1] >= 0)
                return 1;
            return -1;
        }

        private bool ReleaseOpenBolt()
        {
            if (!CanReleaseOpenBolt()) return false;

            int slot = FindOpenBoltRound();
            racked = false;
            spentAmmoTypes.Remove(0);
            if (slot == 1)
            {
                rounds[0] = rounds[slot];
                rounds.RemoveAt(slot);
                rounds.Add(-2);
                if (JamChance())
                {
                    BeginJam(GunJam.Feed, 0, rounds[0]);
                    return false;
                }
            }
            else if (slot < 0)
                rounds[0] = -2;

            UpdateSprite();
            return slot >= 0;
        }

        private void OpenBoltAfterShot()
        {
            openBoltCycling = false;
            racked = true;
            EjectRound(0);
            if (IsJammed) return;

            UpdateSprite();
            if (Plugin.MultiplayerActive) SyncIfHost();
        }
    }
}
