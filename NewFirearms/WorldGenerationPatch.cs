using UnityEngine;
using UnityEngine.Tilemaps;
using HarmonyLib;

namespace NewFirearms
{
    // override to spawn magazines
    [HarmonyPatch(typeof(WorldGeneration), "GenerateCollapsedPods")]
    public class WorldGenerationPatch
    {
        static bool Prefix(WorldGeneration __instance, float amt)
        {
            float num = (float)(__instance.chunkWidth * __instance.chunkHeight) * amt * __instance.totalLootRarity;
            for (int i = 0; (float)i < num; i++)
            {
                Vector2 vector = new Vector2(UnityEngine.Random.Range(0L - (long)__instance.halfWidth + 32, __instance.halfWidth - 32), UnityEngine.Random.Range(0L - (long)__instance.halfWidth + 32, __instance.halfWidth - 32));
                RaycastHit2D raycastHit2D = Physics2D.Raycast(vector, Vector2.down, 400f, LayerMask.GetMask("Ground"));
                if ((bool)raycastHit2D)
                {
                    vector = raycastHit2D.point;
                }
                Vector2Int pos = __instance.WorldToBlockPos(vector);
                for (int j = 0; j < 90; j++)
                {
                    for (int k = 0; k < 90; k++)
                    {
                        float num2 = Vector2.Distance(vector + Vector2.up * k + Vector2.right * j - Vector2.one * 45f, vector);
                        if (num2 < 45f * UnityEngine.Random.Range(0f, 12f / (num2 * 0.8f)) && UnityEngine.Random.Range(0f, 1f) < 0.7f)
                        {
                            Vector2Int vector2Int = __instance.WorldToBlockPos(vector);
                            vector2Int.x += -45 + j;
                            vector2Int.y += -45 + k + 2;
                            if (vector2Int.x < 0)
                            {
                                vector2Int.x = 0;
                            }
                            if (vector2Int.x > __instance.width - 1)
                            {
                                vector2Int.x = (int)(__instance.width - 1);
                            }
                            if (vector2Int.y < 0)
                            {
                                vector2Int.y = 0;
                            }
                            if (vector2Int.y > __instance.height - 1)
                            {
                                vector2Int.y = (int)(__instance.height - 1);
                            }
                            if (__instance.worldBlocks[vector2Int.x, vector2Int.y] > 0)
                            {
                                __instance.worldBlocks[vector2Int.x, vector2Int.y] = (ushort)UnityEngine.Random.Range(0, 5);
                            }
                        }
                    }
                }
                __instance.GenerateObjectAtPos(pos, Resources.Load<GameObject>("LifepodCollapsed").transform.GetChild(0).GetComponent<Tilemap>(), 0.88f, genMode: true);
                if (UnityEngine.Random.value < 0.9f)
                {
                    GameObject component = Utils.Create(Plugin.milkyMags.PickRandom(), vector, UnityEngine.Random.value * 360f);
                    component.GetComponent<Item>().condition = UnityEngine.Random.value;
                }
                for (int l = 0; l < 3; l++)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < 0.3f)
                    {
                        UnityEngine.Object.Instantiate(Resources.Load("experimentflesh"), vector + Vector2.right * UnityEngine.Random.Range(-3f, 3f), Quaternion.identity);
                    }
                }
                if (UnityEngine.Random.Range(0f, 1f) < 0.8f)
                {
                    UnityEngine.Object.Instantiate(Resources.Load("internalorgans"), vector + Vector2.right * UnityEngine.Random.Range(-3f, 3f), Quaternion.identity);
                }
            }
            return false;
        }
    }
}
