#if DEBUG
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GunMinigame
{
    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    internal static class EmbedCargoCommand
    {
        private static void Postfix()
        {
            ConsoleScript.Commands.Add(new Command("nf_embedcargo",
                "Embed cargo in the current player.", args =>
                {
                    if (PlayerCamera.main == null || PlayerCamera.main.body == null)
                    {
                        Debug.LogWarning("[GunMinigame] nf_embedcargo requires an active player.");
                        return;
                    }
                    PlayerCamera.main.body.EmbedCargo();
                }, new Dictionary<int, List<string>>()));
        }
    }
}
#endif
