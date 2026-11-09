using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace NewFirearms
{
    [HarmonyPatch(typeof(Body), "HandleVisuals")]
    internal static class GunAnimationPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var code = new List<CodeInstruction>(instructions);
            var locals = __originalMethod.GetMethodBody().LocalVariables;
            var transformGetter = AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform));
            var fallback = AccessTools.Method(typeof(GunAnimationPatch), nameof(GetGunTransform));
            for (int i = 0; i < code.Count; i++)
            {
                // The release dereferences a GunScript local even for custom gun-tagged items.
                if (i > 0 && code[i].Calls(transformGetter) && LoadsGunScript(code[i - 1], locals))
                {
                    var loadBody = new CodeInstruction(OpCodes.Ldarg_0);
                    var call = new CodeInstruction(code[i]);
                    loadBody.labels.AddRange(call.labels);
                    call.labels.Clear();
                    loadBody.blocks.AddRange(call.blocks);
                    call.blocks.Clear();
                    call.opcode = OpCodes.Call;
                    call.operand = fallback;
                    yield return loadBody;
                    yield return call;
                }
                else yield return code[i];
            }
        }

        private static bool LoadsGunScript(CodeInstruction instruction, IList<LocalVariableInfo> locals)
        {
            int index;
            if (instruction.opcode == OpCodes.Ldloc_0) index = 0;
            else if (instruction.opcode == OpCodes.Ldloc_1) index = 1;
            else if (instruction.opcode == OpCodes.Ldloc_2) index = 2;
            else if (instruction.opcode == OpCodes.Ldloc_3) index = 3;
            else if (instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S)
            {
                if (instruction.operand is LocalVariableInfo local) return local.LocalType == typeof(GunScript);
                index = Convert.ToInt32(instruction.operand);
            }
            else return false;
            return index >= 0 && index < locals.Count && locals[index].LocalType == typeof(GunScript);
        }

        private static Transform GetGunTransform(GunScript gun, Body body)
        {
            if (gun != null) return gun.transform;
            Item held = body.GetItem(body.handSlot);
            return held != null ? held.transform : body.transform;
        }
    }
}
