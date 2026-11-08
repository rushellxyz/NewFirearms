using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        [JsonProperty("magazineBatteries")]
        internal List<BatteryCell> magazineBatteries = new List<BatteryCell>();

        private bool HasBatteryPower => HasBatteryCharge(prop.batteryDrainPerShot);

        private bool HasBatteryCharge(float amount)
        {
            if (amount <= 0f) return true;
            float charge = BatteryCells.Charge(magazineBatteries);
            return !string.IsNullOrEmpty(mag) && charge > 0f && charge + amount * 0.00001f >= amount;
        }

        [System.Runtime.Serialization.OnDeserialized]
        private void NormalizeBatteryCells(System.Runtime.Serialization.StreamingContext context)
        {
            magazineBatteries = BatteryCells.Copy(magazineBatteries);
        }

        internal void SetBatteryState(IEnumerable<BatteryCell> cells)
        {
            magazineBatteries = BatteryCells.Copy(cells);
            if (prop?.feedType == FeedKind.Battery) UpdateBatteryRound();
        }

        private void UpdateBatteryRound()
        {
            // Retain one chamber indicator for the existing minigame and snapshot format.
            sbyte round = HasBatteryPower ? (sbyte)0 : (sbyte)-2;
            if (rounds == null || rounds.Count != 1) rounds = new List<sbyte> { round };
            else rounds[0] = round;
        }

        private void FinishBatteryShot()
        {
            BatteryCells.Drain(magazineBatteries, prop.batteryDrainPerShot);
            UpdateBatteryRound();
            gasTime = Mathf.Max(0.05f, prop.desiredGasTime);
            UpdateSprite();
            if (Plugin.MultiplayerActive) SyncIfHost();
        }
    }
}
