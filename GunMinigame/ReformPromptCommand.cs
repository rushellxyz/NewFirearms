#if DEBUG
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GunMinigame
{
    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    internal static class ReformPromptCommand
    {
        private static void Postfix()
        {
            ConsoleScript.Commands.Add(new Command("nf_reformprompt",
                "Puts player in almost dead position", args =>
                {
                    Body body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
                    if (body == null)
                    {
                        Debug.LogWarning("[GunMinigame] nf_reformprompt requires an active player.");
                        return;
                    }
                    body.heartRate = 0f;
                    body.bloodPressure = 0f;
                    body.consciousness = 0f;
                    body.brainHealth = 1f;
                }, new Dictionary<int, List<string>>()));
        }
    }
}
#endif
