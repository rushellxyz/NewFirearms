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
            bool releaseAim = false;
            for (int i = 1; i < code.Count; i++)
                releaseAim |= code[i].Calls(transformGetter) && LoadsGunScript(code[i - 1], locals);
            var getFloat = AccessTools.Method(typeof(Animator), nameof(Animator.GetFloat), new[] { typeof(string) });
            var setFloat = AccessTools.Method(typeof(Animator), nameof(Animator.SetFloat), new[] { typeof(string), typeof(float) });
            var lerp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), new[] { typeof(float), typeof(float), typeof(float) });
            var worldAngles = AccessTools.PropertySetter(typeof(Transform), nameof(Transform.eulerAngles));
            bool inGunAim = false;
            for (int i = 0; i < code.Count; i++)
            {
                if (releaseAim && code[i].opcode == OpCodes.Ldstr && code[i].operand as string == "gunangle" &&
                    i + 1 < code.Count && code[i + 1].Calls(getFloat))
                    inGunAim = true;
                MethodInfo replacement = null;
                if (i > 0 && code[i].Calls(transformGetter) && LoadsGunScript(code[i - 1], locals))
                    replacement = fallback;
                else if (inGunAim && code[i].Calls(lerp))
                    replacement = AccessTools.Method(typeof(GunAnimationPatch), nameof(LerpGunAim));
                else if (inGunAim && code[i].Calls(worldAngles))
                    replacement = AccessTools.Method(typeof(GunAnimationPatch), nameof(SetGunAimAngles));

                if (replacement != null)
                {
                    var loadBody = new CodeInstruction(OpCodes.Ldarg_0);
                    var call = new CodeInstruction(code[i]);
                    loadBody.labels.AddRange(call.labels);
                    call.labels.Clear();
                    loadBody.blocks.AddRange(call.blocks);
                    call.blocks.Clear();
                    call.opcode = OpCodes.Call;
                    call.operand = replacement;
                    yield return loadBody;
                    yield return call;
                }
                else yield return code[i];
                if (inGunAim && code[i].Calls(setFloat)) inGunAim = false;
            }
        }

        private static Item HeldCustomGun(Body body)
        {
            Item held = body.GetItem(body.handSlot);
            return held != null && held.TryGetComponent<RshGun>(out _) ? held : null;
        }

        private static float LerpGunAim(float current, float target, float amount, Body body)
        {
            if (HeldCustomGun(body) == null) return Mathf.Lerp(current, target, amount);
            // Match CU2's demo: aim from the torso and let the arms pose orient the gun.
            float facing = body.isRight ? 1f : -1f;
            Transform torso = body.limbs[1].transform;
            float torsoAngle = torso.eulerAngles.z;
            if (torsoAngle > 180f) torsoAngle -= 360f;
            float demoTarget = Vector2.SignedAngle(body.transform.right * facing,
                (body.targetLookPos - torso.position).normalized) * facing - torsoAngle * facing;
            return Mathf.Lerp(current, demoTarget, Time.deltaTime * 8f * body.slots[body.handSlot].armPowerMult);
        }

        private static void SetGunAimAngles(Transform target, Vector3 angles, Body body)
        {
            Item held = HeldCustomGun(body);
            if (held != null && target == held.transform)
                target.localEulerAngles = new Vector3(0f, 0f, held.Stats.slotRotation);
            else
                target.eulerAngles = angles;
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
