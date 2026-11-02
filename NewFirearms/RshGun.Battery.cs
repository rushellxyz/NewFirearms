using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        public const int BatteryFeed = 5;

        [JsonProperty("magazineBatteries")]
        internal List<BatteryCell> magazineBatteries = new List<BatteryCell>();

        private bool HasBatteryPower
        {
            get
            {
                float charge = BatteryCells.Charge(magazineBatteries);
                return !string.IsNullOrEmpty(mag) && charge > 0f &&
                    charge + prop.batteryDrainPerShot * 0.00001f >= prop.batteryDrainPerShot;
            }
        }

        [System.Runtime.Serialization.OnDeserialized]
        private void NormalizeBatteryCells(System.Runtime.Serialization.StreamingContext context)
        {
            magazineBatteries = BatteryCells.Copy(magazineBatteries);
        }

        internal void SetBatteryState(IEnumerable<BatteryCell> cells)
        {
            magazineBatteries = BatteryCells.Copy(cells);
            if (prop?.feedType == BatteryFeed) UpdateBatteryRound();
        }

        private void UpdateBatteryRound()
        {
            // Retain one chamber indicator for the existing minigame and snapshot format.
            rounds = new List<sbyte> { HasBatteryPower ? (sbyte)0 : (sbyte)-2 };
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
