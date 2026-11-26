using System.Collections.Generic;
using GunMinigame;
using HarmonyLib;
using UnityEngine;

namespace NewFirearms
{
    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    internal static class AssetMemoryCommand
    {
        private const double BytesPerMiB = 1024.0 * 1024.0;

        private static void Postfix(ConsoleScript __instance)
        {
            ConsoleScript.Commands.Add(new Command("nf_memory",
                ModLocale.Get("newfirearms.command.memory.description"),
                args => Report(__instance), new Dictionary<int, List<string>>()));
        }

        private static void Report(ConsoleScript console)
        {
            var textures = LoadedAssetMemory<Texture>.Measure();
            var audio = LoadedAssetMemory<AudioClip>.Measure(EstimatePcmBytes);
            long totalBytes = textures.bytes + audio.bytes;

            console.LogToConsole(ModLocale.Get("newfirearms.command.memory.textures",
                textures.count, textures.bytes / BytesPerMiB, textures.bytes));
            console.LogToConsole(ModLocale.Get("newfirearms.command.memory.audio",
                audio.count, audio.estimatedBytes / BytesPerMiB, audio.estimatedBytes, audio.bytes));
            console.LogToConsole(ModLocale.Get("newfirearms.command.memory.total",
                totalBytes / BytesPerMiB, totalBytes));
        }

        private static long EstimatePcmBytes(AudioClip clip)
            => clip.loadState == AudioDataLoadState.Loaded
                ? (long)clip.samples * clip.channels * sizeof(float)
                : 0;
    }
}
