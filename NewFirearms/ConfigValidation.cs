using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Ой страшно, я не понимаю как это работает

namespace NewFirearms
{
    internal sealed class ConfigValidationException : Exception
    {
        internal ConfigValidationException(IEnumerable<string> errors, Exception inner = null)
            : base(string.Join("\n", errors.Select(error => "  - " + error)), inner) { }
    }

    internal sealed class ConfigValidation
    {
        private readonly List<string> errors = new List<string>();
        private readonly string folder;
        private readonly Func<string, bool> cachedSprite;

        internal ConfigValidation(string folder, Func<string, bool> cachedSprite)
        {
            this.folder = folder;
            this.cachedSprite = cachedSprite;
        }

        private void Require(bool valid, string field, string expected)
        {
            if (!valid) errors.Add(field + ": " + expected);
        }

        private void Finish()
        {
            if (errors.Count > 0) throw new ConfigValidationException(errors);
        }

        private void Sprite(string path, string field, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Require(!required, field, "a sprite path is required (relative to the item folder, without .png).");
                return;
            }
            if (path.StartsWith("COPY__", StringComparison.Ordinal))
                Require(cachedSprite(path), field, $"cached sprite '{path}' does not exist; check its spelling and loading order.");
            else
                Require(File.Exists(Path.Combine(folder, path + ".png")), field,
                    $"sprite file '{Path.Combine(folder, path + ".png")}' does not exist; paths must omit .png.");
        }

        private void SpriteArray(IList<string> paths, string field, int minimum = 0, bool required = false)
        {
            Require(!required || paths != null, field, $"expected at least {minimum} entries, one per ammo type.");
            if (paths == null) return;
            Require(paths.Count >= minimum, field, $"has {paths.Count} entries; expected at least {minimum}.");
            for (int i = 0; i < paths.Count; i++) Sprite(paths[i], $"{field}[{i}]");
        }

        private void Texture(string path, string field, bool required = false)
        {
            if (path != null && path.StartsWith("COPY__", StringComparison.Ordinal))
                errors.Add(field + ": COPY__ is supported for sprite fields, not texture fields; use a local PNG path without .png.");
            else
                Sprite(path, field, required);
        }

        private void Ids(IList<string> ids, string field, bool required)
        {
            Require(!required || ids != null && ids.Count > 0, field, "expected a non-empty array of item/type IDs.");
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
                Require(!string.IsNullOrWhiteSpace(ids[i]), $"{field}[{i}]", "ID cannot be null or empty.");
        }

        private void Recipe(bool craftable, IList<string> recipe)
        {
            Require(!craftable || recipe != null, "recipe", "an array is required when craftable is true (use [] for no solid ingredients).");
            if (craftable) Ids(recipe, "recipe", false);
        }

        internal void Gun(GunJson gun)
        {
            if (gun == null) throw new ConfigValidationException(new[] { "$: expected a JSON object, got null." });
            Require(gun.feedType >= 1 && gun.feedType <= 4, "feedType", "expected 1 (magazine), 2 (tube), 3 (barrel), or 4 (revolver).");
            Require(gun.internalCapacity > 0, "internalCapacity", "must be greater than zero.");
            Require(gun.feedType != 1 || gun.internalCapacity == 1, "internalCapacity", "must be 1 for magazine-fed guns.");
            Require(gun.ppu > 0 && !float.IsInfinity(gun.ppu), "ppu", "must be a finite number greater than zero.");
            int count = gun.ammoTypes?.Count ?? 0;
            Require(count > 0 && count <= 127, "ammoTypes", $"has {count} entries; expected 1 to 127.");
            for (int i = 0; i < count; i++)
            {
                var ammo = gun.ammoTypes[i];
                Require(ammo != null, $"ammoTypes[{i}]", "expected an ammo object, got null.");
                if (ammo == null) continue;
                Require(!string.IsNullOrWhiteSpace(ammo.round), $"ammoTypes[{i}].round", "an item ID is required.");
                Sprite(ammo.casingSprite, $"ammoTypes[{i}].casingSprite");
                Sprite(ammo.minigameCasingSprite, $"ammoTypes[{i}].minigameCasingSprite");
            }
            Require(gun.fireModes != null && gun.fireModes.Count > 0, "fireModes", "a non-empty array is required.");
            int lastMode = 0;
            if (gun.fireModes != null)
                for (int i = 0; i < gun.fireModes.Count; i++)
                {
                    Require(gun.fireModes[i] <= 3, $"fireModes[{i}]", "expected 0 (safe), 1 (manual), 2 (semi-auto), or 3 (full-auto).");
                    lastMode = Math.Max(lastMode, gun.fireModes[i]);
                }
            Require(gun.feedType != 1 || gun.magazineType != null, "magazineType", "an array is required for magazine-fed guns.");
            Ids(gun.magazineType, "magazineType", false);
            Recipe(gun.craftable, gun.recipe);
            Texture(gun.normalTexturePath, "normalTexturePath", true);
            Texture(gun.rackedTexturePath, "rackedTexturePath", true);
            if (gun.bulletsSpritesPath?.Count > 0)
            {
                Require(gun.bulletsSpritesPath.Count >= count + 2, "bulletsSpritesPath",
                    $"has {gun.bulletsSpritesPath.Count} entries; expected at least {count + 2}: empty, spent casing, then one sprite for each of {count} ammoTypes entries.");
                SpriteArray(gun.bulletsSpritesPath, "bulletsSpritesPath");
            }
            if (gun.fireModesSpritePath?.Count > 0)
                SpriteArray(gun.fireModesSpritePath, "fireModesSpritePath", lastMode + 1);
            Sprite(gun.rackSpritePath, "rackSpritePath");
            Sprite(gun.unrackSpritePath, "unrackSpritePath");
            Sprite(gun.removeMagSpritePath, "removeMagSpritePath");
            if (gun.magazineTexturesPath != null)
                foreach (var pair in gun.magazineTexturesPath) Texture(pair.Value, $"magazineTexturesPath['{pair.Key}']");
            if (gun.minigame != null) Minigame(gun.minigame, count, lastMode);
            Finish();
        }

        private void Minigame(JsonMinigameInfo game, int count, int lastMode)
        {
            Sprite(game.mainExperimentPath, "minigame.mainExperimentPath", true);
            Sprite(game.mainMilkyTriggerOnPath, "minigame.mainMilkyTriggerOnPath", true);
            Sprite(game.mainMilkyTriggerOffPath, "minigame.mainMilkyTriggerOffPath", true);
            Sprite(game.mainDunePath, "minigame.mainDunePath", true);
            Require(game.ammos == null || game.ammos.Length == count, "minigame.ammos", $"must have {count} entries to match ammoTypes, or be omitted to generate automatically.");
            Require(game.dCurrentAmmo < count, "minigame.dCurrentAmmo", "must index ammoTypes; omit this runtime field to start at the first ammo type.");
            Require(!game.initialized, "minigame.initialized", "must be omitted or false; this is runtime state, not a config option.");
            SpriteArray(game.ammosInHandPaths, "minigame.ammosInHandPaths", count, true);
            SpriteArray(game.ammosInPtrPaths, "minigame.ammosInPtrPaths", count, true);
            if (game.casingsPath?.Length > 0) SpriteArray(game.casingsPath, "minigame.casingsPath", count + 1);
            if (game.fireModePaths?.Length > 0) SpriteArray(game.fireModePaths, "minigame.fireModePaths", lastMode + 1);
            SpriteArray(game.coverAnimationPaths, "minigame.coverAnimationPaths");
            Sprite(game.sliderFrontPath, "minigame.sliderFrontPath");
            Sprite(game.sliderBackPath, "minigame.sliderBackPath");
            Sprite(game.backLayerPath, "minigame.backLayerPath");
            Sprite(game.rackedOnlyPath, "minigame.rackedOnlyPath");
            Sprite(game.unrackedOnlyPath, "minigame.unrackedOnlyPath");
            Sprite(game.receiverPath, "minigame.receiverPath");
            Sprite(game.magReleasePath, "minigame.magReleasePath");
            Sprite(game.magazineDragPath, "minigame.magazineDragPath");
            Sprite(game.laserTogglePath, "minigame.laserTogglePath");
            Sprite(game.laserPath, "minigame.laserPath");
            Sprite(game.fireTogglePath, "minigame.fireTogglePath");
            Require(game.sliderMinimumPosition <= game.sliderMaximumPosition, "minigame.sliderMinimumPosition", "must not exceed sliderMaximumPosition.");
            Require(game.rackPoint >= game.sliderMinimumPosition && game.rackPoint <= game.sliderMaximumPosition,
                "minigame.rackPoint", "must be between sliderMinimumPosition and sliderMaximumPosition.");
            Require(!game.canUseFannyPack || game.magazinePaths != null, "minigame.magazinePaths", "a dictionary is required when canUseFannyPack is true (use {} for no magazines).");
            if (game.magazinePaths != null)
                foreach (var pair in game.magazinePaths)
                {
                    Sprite(pair.Value, $"minigame.magazinePaths['{pair.Key}']");
                    if (game.ammosInMagazinePaths != null)
                        Require(game.ammosInMagazinePaths.ContainsKey(pair.Key), $"minigame.ammosInMagazinePaths['{pair.Key}']", "missing entry for a magazine listed in magazinePaths.");
                }
            if (game.ammosInMagazinePaths != null)
                foreach (var pair in game.ammosInMagazinePaths)
                    SpriteArray(pair.Value, $"minigame.ammosInMagazinePaths['{pair.Key}']", count, true);
        }

        internal void Magazine(MagJson mag)
        {
            if (mag == null) throw new ConfigValidationException(new[] { "$: expected a JSON object, got null." });
            Require(mag.capacity > 0 && mag.capacity <= ushort.MaxValue, "capacity", "must be between 1 and 65535.");
            Ids(mag.ammoTypes, "ammoTypes", true);
            Require(mag.ammoTypes == null || mag.ammoTypes.Count <= 127, "ammoTypes", "cannot contain more than 127 ammo types.");
            Require(mag.magazineType != null, "magazineType", "an array is required; use [] for ammo boxes that cannot be inserted into guns.");
            Ids(mag.magazineType, "magazineType", false);
            Recipe(mag.craftable, mag.recipe);
            Sprite("item", "item sprite", true);
            Sprite(mag.nonEmptySpritePath, "nonEmptySpritePath");
            Finish();
        }

        internal void Round(RoundJson round)
        {
            if (round == null) throw new ConfigValidationException(new[] { "$: expected a JSON object, got null." });
            Recipe(round.craftable, round.recipe);
            Require(!round.craftable || round.craftAmount > 0, "craftAmount", "must be greater than zero when craftable is true.");
            Sprite("item", "item sprite", true);
            Finish();
        }
    }
}
