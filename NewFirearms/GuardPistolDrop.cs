using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace NewFirearms
{
    [HarmonyPatch]
    internal static class GuardPistolDropPatch
    {
        private static MethodBase target;

        private static bool Prepare()
        {
            // GuardScript exists only in the full game.
            var guard = AccessTools.TypeByName("GuardScript");
            target = guard == null ? null : AccessTools.DeclaredMethod(guard, "Die", Type.EmptyTypes);
            return target != null;
        }

        private static MethodBase TargetMethod() => target;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var code = new List<CodeInstruction>(instructions);
            var create = AccessTools.Method(typeof(Utils), nameof(Utils.Create), new[] { typeof(string), typeof(Vector2), typeof(float) });
            var ammo = AccessTools.Field(__originalMethod.DeclaringType, "ammo");
            var hasMagField = AccessTools.Field(typeof(GunScript), nameof(GunScript.hasMag));
            var magazineRoundsField = AccessTools.Field(typeof(GunScript), nameof(GunScript.roundsInMag));
            var chamberField = AccessTools.Field(typeof(GunScript), nameof(GunScript.roundInChamber));
            int hasMagazine = code.FindIndex(instruction => instruction.StoresField(hasMagField));
            int magazineRounds = code.FindIndex(instruction => instruction.StoresField(magazineRoundsField));
            int chamber = code.FindIndex(instruction => instruction.StoresField(chamberField));
            int spawn = hasMagazine < 0 ? -1 : code.FindLastIndex(hasMagazine, instruction => instruction.Calls(create));
            if (spawn < 0 || ammo == null || spawn + 4 >= hasMagazine ||
                code[spawn + 1].opcode != OpCodes.Dup || code[spawn + 3].opcode != OpCodes.Ldarg_0 ||
                !code[spawn + 4].LoadsField(ammo) ||
                magazineRounds <= hasMagazine || chamber <= magazineRounds)
            {
                Debug.LogWarning("[NewFirearms] Could not find the guard's pistol initialization.");
                return code;
            }

            // The pistol GameObject is on the stack. Replace only the three GunScript writes.
            var loadGuard = new CodeInstruction(OpCodes.Ldarg_0);
            loadGuard.labels.AddRange(code[spawn + 1].labels);
            loadGuard.blocks.AddRange(code[spawn + 1].blocks);
            code.RemoveRange(spawn + 1, chamber - spawn);
            code.InsertRange(spawn + 1, new[]
            {
                loadGuard,
                new CodeInstruction(OpCodes.Ldfld, ammo),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GuardPistolDropPatch), nameof(InitializeDrop))),
            });
            return code;
        }

        private static void InitializeDrop(GameObject pistol, int ammo)
        {
            if (pistol == null || RshComponent.IsClient()) return;
            if (pistol.TryGetComponent<Item>(out var item))
                item.condition = UnityEngine.Random.Range(0.5f, 0.9f);
            if (Plugin.GunDefinitions.ContainsKey("pistol"))
            {
                // CUCoreLib may install RshGun in Item.Start, after the guard's death method returns.
                pistol.AddComponent<GuardPistolContents>().Ammo = ammo;
            }
            else if (pistol.TryGetComponent<GunScript>(out var native))
            {
                native.hasMag = ammo >= 0;
                native.roundsInMag = ammo > 0 ? ammo - 1 : 0;
                native.roundInChamber = ammo > 0 ? GunScript.RoundInChamber.Round : GunScript.RoundInChamber.None;
            }
        }
    }

    internal sealed class GuardPistolContents : MonoBehaviour
    {
        internal int Ammo;

        internal static bool Apply(RshGun gun, int ammo)
        {
            if (gun.prop.feedType != FeedKind.Magazine)
                return false;

            if (ammo >= 0)
            {
                const string magazineId = "smallmagazine";
                int roundIndex = gun.prop.ammoTypes.FindIndex(round => round.round == "9mmround");
                if (roundIndex < 0 || roundIndex > sbyte.MaxValue ||
                    !Plugin.MagazineDefinitions.TryGetValue(magazineId, out var magazine) || !gun.CanFitMag(magazine))
                {
                    Debug.LogWarning("[NewFirearms] The guard's pistol needs compatible smallmagazine and 9mmround definitions.");
                    return false;
                }

                gun.mag = magazineId;
                gun.rounds[0] = ammo > 0 ? (sbyte)roundIndex : (sbyte)-2;
                for (int i = 1; i <= magazine.capacity; i++)
                    gun.rounds.Add(i < ammo ? (sbyte)roundIndex : (sbyte)-2);
            }
            gun.racked = false;
            return true;
        }
    }
}
