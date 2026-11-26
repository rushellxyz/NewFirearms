using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GunMinigame
{
    // Both plugins record the objects created by their file loaders.
    public static class LoadedAssetMemory<T> where T : Object
    {
        private static readonly HashSet<int> ids = new HashSet<int>();

        // Instance IDs let Unity unload unused assets normally.
        public static void Track(T asset) => ids.Add(asset.GetInstanceID());

        public static (int count, long bytes, long estimatedBytes) Measure(System.Func<T, long> estimate = null)
        {
            var unloaded = new List<int>();
            int count = 0;
            long bytes = 0;
            long estimatedBytes = 0;
            foreach (int id in ids)
            {
                if (!(Resources.InstanceIDToObject(id) is T asset) || asset == null)
                {
                    unloaded.Add(id);
                    continue;
                }

                count++;
                bytes += Profiler.GetRuntimeMemorySizeLong(asset);
                if (estimate != null)
                    estimatedBytes += estimate(asset);
            }

            ids.ExceptWith(unloaded);
            return (count, bytes, estimatedBytes);
        }
    }
}
