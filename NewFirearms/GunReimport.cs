using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GunMinigame;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace NewFirearms
{
    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    internal static class GunReimport
    {
        private static void Postfix()
        {
            ConsoleScript.Commands.Add(new Command("nf_reload", ModLocale.Get("newfirearms.command.reload.description"),
                Run, new Dictionary<int, List<string>> { [1] = Plugin.GunDefinitions.Keys.OrderBy(id => id).ToList() }));
        }

        internal static void Run(string[] args)
        {
            if (Plugin.MultiplayerActive)
            {
                Debug.LogWarning(ModLocale.Get("newfirearms.command.reload.multiplayer"));
                return;
            }
            var body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
            string[] parameters = args?.Skip(1).ToArray() ?? new string[0];
            bool destroyExisting = parameters.Contains("--destroy");
            string[] ids = parameters.Where(arg => arg != "--destroy").ToArray();
            string id = ids.Length > 0 ? ids[0] : body?.GetItem(body.handSlot)?.id;
            if (parameters.Length > 2 || ids.Length > 1 || string.IsNullOrWhiteSpace(id) ||
                id != Path.GetFileName(id) || id.Contains("\\") || !Plugin.GunDefinitions.TryGetValue(id, out var previous))
            {
                Debug.LogWarning(ModLocale.Get("newfirearms.command.reload.usage"));
                return;
            }

            var manager = MinigameManager.instance;
            var shownGun = manager != null && manager.active ? manager.gun as RshGun : null;
            bool refreshWindow = shownGun != null && shownGun.it != null && shownGun.it.id == id;
            if (refreshWindow && (manager.IsManipulatingGun || manager.holding != null || manager.holdingInMinigame != null))
            {
                Debug.LogWarning(ModLocale.Get("newfirearms.command.reload.busy"));
                return;
            }

            string folder = Path.Combine("BepInEx/plugins/NewFirearms/Guns", id);
            try
            {
                var next = JsonConvert.DeserializeObject<GunJson>(File.ReadAllText(Path.Combine(folder, "gun.json")));
                var guns = Resources.FindObjectsOfTypeAll<RshGun>()
                    .Where(gun => gun.it != null && gun.it.id == id && gun.gameObject.scene.IsValid()).ToArray();
                if (!destroyExisting && guns.Length > 0 && !HasCompatibleState(previous, next))
                {
                    Debug.LogWarning(ModLocale.Get("newfirearms.command.reload.structure"));
                    return;
                }

                ItemLoader.BeginReimport();
                try
                {
                    // Prepare every asset and validate the config before replacing the registry entry.
                    ItemLoader.PrepareGun(folder, next);
                    if (!destroyExisting && previous.minigame != null && next.minigame != null)
                        next.minigame.dCurrentAmmo = (ushort)Math.Min(previous.minigame.dCurrentAmmo, next.ammoTypes.Count - 1);
                    var oldRecipe = Plugin.recipes.FirstOrDefault(recipe => recipe.result.id == id);
                    ItemLoader.RegisterPreparedGun(id, next);
                    RefreshRecipe(oldRecipe, Plugin.recipes.FirstOrDefault(recipe => recipe.result.id == id));
                    VanillaGunProxy.FinalizeDefinitions();
                    if (refreshWindow) manager.Hide();
                    foreach (var gun in guns)
                    {
                        if (destroyExisting)
                        {
                            var container = gun.it.ParentContainer();
                            if (container != null) container.UnloadItem(gun.it);
                            gun.gameObject.SetActive(false);
                            UnityEngine.Object.Destroy(gun.gameObject);
                        }
                        else gun.ApplyReimport(next);
                    }
                    if (!destroyExisting && refreshWindow && next.minigame != null)
                        manager.Show(next.minigame, shownGun, shownGun.GetComponent<LaserPointer>());
                    Debug.Log(ModLocale.Get(destroyExisting ? "newfirearms.command.reload.destroyed" :
                        "newfirearms.command.reload.success", id, guns.Length));
                }
                finally
                {
                    ItemLoader.EndReimport();
                    // Old definitions and cached templates no longer own their assets.
                    Resources.UnloadUnusedAssets();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ModLocale.Get("newfirearms.command.reload.failed", id, ex.Message));
            }
        }

        internal static bool HasCompatibleState(GunJson previous, GunJson next)
            => next != null && previous.feedType == next.feedType &&
                previous.internalCapacity == next.internalCapacity && previous.hasLaser == next.hasLaser &&
                previous.needRetract == next.needRetract && next.ammoTypes != null &&
                previous.ammoTypes.Select(ammo => ammo.round).SequenceEqual(next.ammoTypes.Select(ammo => ammo?.round)) &&
                (previous.magazineType ?? new List<string>()).SequenceEqual(next.magazineType ?? new List<string>());

        private static void RefreshRecipe(Recipe previous, Recipe next)
        {
            if (Recipes.recipes == null) return;
            int index = previous != null ? Recipes.recipes.IndexOf(previous) : -1;
            if (next != null)
            {
                next.hasMadeBefore = previous?.hasMadeBefore ?? false;
                next.specialKnown = previous?.specialKnown ?? false;
                if (index >= 0) Recipes.recipes[index] = next;
                else Recipes.recipes.Add(next);
            }
            else if (index >= 0) Recipes.recipes.RemoveAt(index);
            for (int i = 0; i < Recipes.recipes.Count; i++) Recipes.recipes[i].index = i;
        }
    }
}
