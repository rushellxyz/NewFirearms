using System.Collections.Generic;

namespace NewFirearms
{
    internal static class BatteryNetwork
    {
        internal static void Write(LiteNetLib.Utils.NetDataWriter writer, List<BatteryCell> cells)
        {
            writer.Put((ushort)(cells?.Count ?? 0));
            if (cells == null) return;
            foreach (var cell in cells)
            {
                writer.Put(cell.id);
                writer.Put(cell.charge);
            }
        }

        internal static List<BatteryCell> Read(LiteNetLib.Utils.NetDataReader reader)
        {
            var cells = new List<BatteryCell>();
            reader.Get(out ushort count);
            for (int i = 0; i < count; i++)
            {
                reader.Get(out string id);
                reader.Get(out float charge);
                cells.Add(new BatteryCell { id = id, charge = charge });
            }
            return BatteryCells.Copy(cells);
        }
    }
}
