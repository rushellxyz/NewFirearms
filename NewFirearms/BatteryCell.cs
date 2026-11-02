using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NewFirearms
{
    // Нету battery was favourited но и пофиг
    public sealed class BatteryCell
    {
        public string id;
        public float charge;
    }

    internal static class BatteryCells
    {
        internal static List<BatteryCell> Copy(IEnumerable<BatteryCell> cells)
            => cells == null ? new List<BatteryCell>() : cells.Where(cell => cell != null && !string.IsNullOrEmpty(cell.id))
                .Select(cell => new BatteryCell { id = cell.id, charge = Mathf.Clamp01(cell.charge) }).ToList();

        internal static float Charge(IList<BatteryCell> cells)
            => cells == null ? 0f : cells.Sum(cell => Mathf.Clamp01(cell.charge));

        internal static float Condition(IList<BatteryCell> cells, int capacity)
            => Mathf.Clamp01(Charge(cells) / Mathf.Max(1, capacity));

        internal static void Drain(IList<BatteryCell> cells, float amount)
        {
            foreach (var cell in cells)
            {
                float used = Mathf.Min(cell.charge, amount);
                cell.charge = Mathf.Max(0f, cell.charge - used);
                amount -= used;
                if (amount <= 0f) break;
            }
        }
    }
}
