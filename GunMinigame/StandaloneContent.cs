using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GunMinigame
{
    internal static class StandaloneContent
    {
        internal static readonly Dictionary<string, MinigameConfig> Guns = new Dictionary<string, MinigameConfig>();

        internal static void Load(string root, Func<string, Sprite> loadSprite, Action<string> warning)
        {
            Guns.Clear();
            var cache = new Dictionary<string, Sprite>();
            LoadGun("pistol", "9mmround", root, loadSprite, cache, warning);
            LoadGun("shotgun", "12gauge", root, loadSprite, cache, warning);
            LoadGun("rifle", "556round", root, loadSprite, cache, warning);
        }

        private static void LoadGun(string id, string ammo, string root, Func<string, Sprite> loadSprite,
            Dictionary<string, Sprite> cache, Action<string> warning)
        {
            try
            {
                string folder = Path.Combine(root, "Guns", id);
                JObject json = JObject.Parse(File.ReadAllText(Path.Combine(folder, "gun.json")));
                var game = json["minigame"]?.ToObject<MinigameConfig>();
                if (game == null) throw new InvalidDataException("minigame object is missing.");
                Sprite Sprite(string path)
                {
                    if (string.IsNullOrWhiteSpace(path)) return null;
                    string file = path.StartsWith("COPY__", StringComparison.Ordinal)
                        ? Path.Combine(root, path.Substring(6) + ".png") : Path.Combine(folder, path + ".png");
                    file = Path.GetFullPath(file);
                    if (!File.Exists(file)) throw new FileNotFoundException("Sprite is missing.", file);
                    if (!cache.TryGetValue(file, out var sprite))
                    {
                        sprite = loadSprite(file);
                        if (sprite == null) throw new InvalidDataException("Could not load sprite: " + file);
                        cache.Add(file, sprite);
                    }
                    return sprite;
                }
                Sprite[] Sprites(string[] paths)
                    => paths == null ? null : Array.ConvertAll(paths, Sprite);
                Sprite[] First(string[] paths)
                    => paths == null || paths.Length == 0 ? null : new[] { Sprite(paths[0]) };

                game.mainExperimentSprite = Sprite(game.mainExperimentPath);
                game.mainMilkyTriggerOnSprite = Sprite(game.mainMilkyTriggerOnPath);
                game.mainMilkyTriggerOffSprite = Sprite(game.mainMilkyTriggerOffPath);
                game.mainDuneSprite = Sprite(game.mainDunePath);
                if (game.mainExperimentSprite == null || game.mainMilkyTriggerOnSprite == null ||
                    game.mainMilkyTriggerOffSprite == null || game.mainDuneSprite == null)
                    throw new InvalidDataException("All four main player-species sprites are required.");
                game.sliderFrontSprite = Sprite(game.sliderFrontPath);
                game.sliderBackSprite = Sprite(game.sliderBackPath);
                game.backLayerSprite = Sprite(game.backLayerPath);
                game.barrelSprite = Sprite(game.barrelPath);
                game.rackedOnlySprite = Sprite(game.rackedOnlyPath);
                game.unrackedOnlySprite = Sprite(game.unrackedOnlyPath);
                game.receiverTrigger = Sprite(game.receiverPath);
                game.magReleaseTrigger = Sprite(game.magReleasePath);
                game.rackTrigger = Sprite(game.rackTriggerPath);
                game.magazineDragTrigger = Sprite(game.magazineDragPath);
                game.fireToggleTrigger = Sprite(game.fireTogglePath);
                // A vanilla safety must remain operable even without a separate switch hit area.
                if (game.fireToggleTrigger == null) game.sliderRightClickFireToggle = true;
                // Inventory safety icons are not slider artwork; only use explicit minigame sprites.
                game.fireModeSprites = Sprites(game.fireModePaths);
                game.coverAnimation = Sprites(game.coverAnimationPaths);
                game.grabTriggerSprite = Sprite(game.grabTrigger);
                if (game.grabOnSlider || game.grabTriggerSprite != null)
                    game.grabHandSprite = Sprite("COPY__Resources/minigameGrab" + game.grabType);

                // Vanilla AmmoScript supports one ammo type per weapon; omit NewFirearms-only alternatives.
                game.ammos = new[] { ammo };
                game.ammosInHand = First(game.ammosInHandPaths);
                game.ammosInPtr = First(game.ammosInPtrPaths);
                string[] icons = json["bulletsSpritesPath"]?.ToObject<string[]>();
                game.ammosIcons = new[] { Sprite(icons != null && icons.Length > 2 ? icons[2] : "COPY__Resources/" + ammo) };
                if (game.casingsPath == null || game.casingsPath.Length < 2)
                    throw new InvalidDataException("casingsPath needs a spent casing and a vanilla round sprite.");
                string spent = (string)json["ammoTypes"]?[0]?["minigameCasingSprite"];
                game.casingsSprite = new[] { Sprite(spent ?? game.casingsPath[0]), Sprite(game.casingsPath[1]) };
                game.magazineSprites = new Dictionary<string, Sprite>();
                if (game.magazinePaths != null)
                    foreach (var entry in game.magazinePaths) game.magazineSprites.Add(entry.Key, Sprite(entry.Value));
                game.ammosInMagazine = new Dictionary<string, Sprite[]>();
                if (game.ammosInMagazinePaths != null)
                    foreach (var entry in game.ammosInMagazinePaths)
                        game.ammosInMagazine.Add(entry.Key, First(entry.Value));
                game.dCurrentAmmo = 0;
                game.initialized = false;
                Guns.Add(id, game);
            }
            catch (Exception ex)
            {
                warning("[GunMinigame] Standalone UI for '" + id + "' is unavailable; keeping vanilla UI. " + ex.Message);
            }
        }
    }
}
