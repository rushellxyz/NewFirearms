using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    internal static class NetworkAction
    {
        public static void DenyAction(Together.ScavPlayer plr, string msg, bool canBeFixedWithSwitchingStrictSync=false)
        {
            if (canBeFixedWithSwitchingStrictSync)
                UnityEngine.Debug.LogWarning($"[NewFirearms] {plr}: {msg}\nThis can be fixing by toggling strictSync in Settings.json");
       else     UnityEngine.Debug.LogWarning($"[NewFirearms] {plr}: {msg}");
            plr.Server_DoAlertSingle(msg);
        }
    }
}
