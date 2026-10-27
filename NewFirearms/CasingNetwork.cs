using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    internal static class CasingNetwork
    {
        internal static void ClientSync(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            string definitionId = reader.GetString();
            reader.Get(out sbyte index);
            // Repeat snapshots cover spawn races and players joining after ejection.
            if (Together.ItemSync.TryGetItem(new Together.knetid(syncId), out _, out var item) &&
                item != null && item.id == "casing")
                CasingAppearance.Attach(item.gameObject, definitionId, index);
        }
    }
}
