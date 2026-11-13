using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace GunMinigame
{
    [HarmonyPatch(typeof(PlayerCamera), "HandleInput")]
    internal static class MarksmanGunHotkeysPatch
    {
        private static bool nativeRackHotkey, nativeUnloadHotkey;
        // Route Milky's native hotkey calls through the animation, without also
        // operating the gun immediately. Other callers keep the normal gun API.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            nativeRackHotkey = nativeUnloadHotkey = false;
            var rack = AccessTools.Method(typeof(PlayerCamera), nameof(PlayerCamera.GunRack));
            var unload = AccessTools.Method(typeof(PlayerCamera), nameof(PlayerCamera.GunEjectMag));
            var rackHotkey = AccessTools.Method(typeof(MarksmanGunHotkeysPatch), nameof(RackHotkey));
            var unloadHotkey = AccessTools.Method(typeof(MarksmanGunHotkeysPatch), nameof(UnloadHotkey));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(rack) || instruction.Calls(unload))
                {
                    if (instruction.Calls(rack)) nativeRackHotkey = true;
                    else nativeUnloadHotkey = true;
                    var replacement = new CodeInstruction(instruction);
                    replacement.opcode = OpCodes.Call;
                    replacement.operand = instruction.Calls(rack) ? rackHotkey : unloadHotkey;
                    yield return replacement;
                }
                else
                    yield return instruction;
            }
        }

        static void Postfix(PlayerCamera __instance)
        {
            // The demo has no native marksman input. The release already reads
            // these bindings for Milky; avoid executing those actions twice.
            if (__instance != PlayerCamera.main || __instance.body == null ||
                !Plugin.IsMarksman(__instance.body))
                return;

            bool milky = Plugin.GetPlayerSpecies(__instance.body) == PlayerSpecies.Milky;
            if ((!milky || !nativeRackHotkey) && Input.GetKeyDown(KeyBinds.GetBind("marksmanrack")))
                RackHotkey(__instance);
            if ((!milky || !nativeUnloadHotkey) && Input.GetKeyDown(KeyBinds.GetBind("marksmanunload")))
                UnloadHotkey(__instance);
        }

        static void RackHotkey(PlayerCamera player) => HandleHotkey(player, true);
        static void UnloadHotkey(PlayerCamera player) => HandleHotkey(player, false);

        static void HandleHotkey(PlayerCamera player, bool rack)
        {
            if (player != PlayerCamera.main || player.body == null ||
                !Plugin.IsMarksman(player.body) || MinigameManager.IsAnyGameUIOpen() ||
                !player.body.HoldingItem(player.body.handSlot))
                return;

            Item item = player.body.GetItem(player.body.handSlot);
            if (!item.Stats.HasTag("gun"))
                return;

            MinigameManager manager = MinigameManager.instance;
            IMinigameGun heldGun = item.GetComponent(typeof(IMinigameGun)) as IMinigameGun;
            if (rack && heldGun is IMinigameRetractableGun retractable && retractable.UsesRetraction())
            {
                if (player.body.conscious) heldGun.Rack();
                return;
            }
            if (manager != null && manager.active)
            {
                if (ReferenceEquals(heldGun, manager.gun) && !manager.hiden)
                {
                    manager.HandleMarksmanHotkey(rack);
                    return;
                }
                if (!ReferenceEquals(heldGun, manager.gun))
                    manager.Hide();
            }

            if (rack)
                player.GunRack();
            else
                player.GunEjectMag();
        }
    }
}
