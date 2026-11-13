using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace GunMinigame
{
    internal static class NativeGameFeatures
    {
        private static readonly bool hasCutscenes = AccessTools.Field(typeof(WorldGeneration), "cutsceneActive") != null;
        private static readonly bool hasEmbedCargo = AccessTools.Method(typeof(Body), "EmbedCargo", Type.EmptyTypes) != null;

        internal static bool IsCutsceneActive()
            => hasCutscenes && ReadCutsceneActive();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadCutsceneActive()
            => WorldGeneration.world != null && WorldGeneration.world.cutsceneActive;

        internal static bool TryEmbedCargo(Body body)
        {
            if (!hasEmbedCargo || body == null) return false;
            EmbedCargo(body);
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EmbedCargo(Body body) => body.EmbedCargo();
    }
}
