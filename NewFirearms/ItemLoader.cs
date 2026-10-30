using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace NewFirearms
{
    public static class ItemLoader
    {
        private static Dictionary<string, Sprite> sprites;

        public static AudioClip shootAudioFallback;
        public static AudioClip unshootAudioFallback;
        public static AudioClip jamAudioFallback;
        public static AudioClip rackAudioFallback;
        public static AudioClip unrackAudioFallback;
        public static AudioClip loadRoundAudioFallback;
        public static AudioClip loadMagAudioFallback;
        public static AudioClip removeMagAudioFallback;
        public static AudioClip toggleFireModeAudioFallback;
        public static AudioClip unloadRoundAudioFallback;
        public static Sprite rackSpriteFallback;
        public static Sprite unrackSpriteFallback;
        public static Sprite removeMagSpriteFallback;
        public static List<Sprite> fireModesFallback;
        public static Sprite[] bulletsSpritesFallback;
        public static PlayerDamage playerDamageFallback;
        public static Sprite emptySprite;

        public static void SetupResourcesDict()
        {
            sprites = new Dictionary<string, Sprite>();
        }

        public static void ClearResourcesDict()
        {
            sprites = null;
        }

        public static void MigrateLegacyPrefixes(string root)
        {
            int migrated = 0;
            foreach (string category in new[] { "Guns", "Mags" })
            {
                string directory = Path.Combine(root, category);
                if (!Directory.Exists(directory)) continue;
                foreach (string path in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        string text = File.ReadAllText(path);
                        string updated = text.Replace("newfirearms.", "nf.");
                        foreach (string ammo in new[] { "9mmhollow", "9mmvenomus", "12gaugebird", "12gaugeslug" })
                            updated = updated.Replace("\"" + ammo + "\"", "\"nf." + ammo + "\"");
                        if (updated == text) continue;
                        File.WriteAllText(path, updated);
                        migrated++;
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        UnityEngine.Debug.LogError($"[NewFirearms] Could not migrate prefixes in '{path}': {ex.Message}");
                    }
                }
            }
            if (migrated > 0)
                UnityEngine.Debug.Log($"[NewFirearms] Migrated legacy item prefixes in {migrated} JSON configs.");
        }

        public static void LoadVanillaAmmoSprites()
        {
            foreach (string id in new[] { "12gauge", "9mmround", "556round" })
            {
                Sprite sprite = LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/" + id);
                if (sprite == null) continue;
                CUCoreLib.Helpers.CUCoreUtils.EditVanillaItem(id, info =>
                    Plugin.RegisterRoundCuCore(id, info, sprite));
            }
        }

        public static void LoadFallbackResources()
        {
            emptySprite = LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/bulletEmpty");
            shootAudioFallback = Resources.Load<AudioClip>("sounds/gunrifleshot");
            unshootAudioFallback = Resources.Load<AudioClip>("sounds/guntrigger");
            jamAudioFallback = Resources.Load<AudioClip>("sounds/gunjam");
            rackAudioFallback = Resources.Load<AudioClip>("sounds/gunrack");
            unrackAudioFallback = Resources.Load<AudioClip>("sounds/gununrack");
            loadRoundAudioFallback = Resources.Load<AudioClip>("sounds/gunloadshell");
            loadMagAudioFallback = Resources.Load<AudioClip>("sounds/gunloadmag");
            removeMagAudioFallback = Resources.Load<AudioClip>("sounds/gununloadmag");
            toggleFireModeAudioFallback = Resources.Load<AudioClip>("sounds/gunsafety");
            unloadRoundAudioFallback = Resources.Load<AudioClip>("sounds/gunloadshell");
            rackSpriteFallback = LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/rack");
            unrackSpriteFallback = LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/unrack");
            removeMagSpriteFallback = LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/removeMag");
            fireModesFallback = new List<Sprite>
            {
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/fireSafe"),
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/firePump"),
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/fireSemi"),
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/fireFull"),
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/fireBurst"),
            };
            bulletsSpritesFallback = new Sprite[]
            {
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/bulletEmpty"),
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/bulletCasing"),
                LoadSprite("BepInEx/plugins/NewFirearms/", "Resources/bulletRound"),
            };
            playerDamageFallback = new PlayerDamage
            {
                traumaAmount = 4f,
                pain = 100f,
                adrenaline = 100f,
                skinDamage = 85f,
                muscleDamage = 85f,
                bleedAmount = 35f,
                wearableDamage = 1f,
                shrapnelChance = 1f,
                fractureChance = 0.3f,
                internalBleed = 20f,
                brainDamage = 45f,
                disfigureChance = 1f,
            };
        }

        private static ConfigValidation CreateValidation(string folder)
            => new ConfigValidation(folder, key => sprites != null && sprites.ContainsKey(key));

        private static void LoadConfigs<T>(string category, string fileName, Action<string, string, T> load)
        {
            string directory = Path.Combine("BepInEx/plugins/NewFirearms", category);
            if (!Directory.Exists(directory))
            {
                UnityEngine.Debug.LogWarning($"[NewFirearms] Content directory '{directory}' does not exist.");
                return;
            }
            string[] folders = Directory.GetDirectories(directory);
            if (category == "Guns") Array.Sort(folders);
            foreach (string folder in folders)
            {
                string jsonPath = Path.Combine(folder, fileName);
                if (!File.Exists(jsonPath))
                {
                    UnityEngine.Debug.LogWarning($"[NewFirearms] '{folder}' contains no {fileName}.");
                    continue;
                }
                try
                {
                    T prop = JsonConvert.DeserializeObject<T>(File.ReadAllText(jsonPath));
                    load(Path.GetFileName(folder), folder, prop);
                }
                catch (ConfigValidationException ex)
                {
                    UnityEngine.Debug.LogError($"[NewFirearms] Invalid config '{jsonPath}':\n{ex.Message}");
                }
                catch (JsonException ex)
                {
                    UnityEngine.Debug.LogError($"[NewFirearms] Invalid JSON in '{jsonPath}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[NewFirearms] Error loading '{jsonPath}':\n{ex}");
                }
            }
        }

        public static void LoadGuns()
        {
            Plugin.milkyGuns = new List<string>();
            LoadConfigs<GunJson>("Guns", "gun.json", LoadGun);
        }

        public static void LoadGun(string itemId, string folder, GunJson prop)
        {
            CreateValidation(folder).Gun(prop);
            for (int i = 0; i < prop.ammoTypes.Count; i++)
            {
                AmmoInfo ammo = prop.ammoTypes[i];
                if (ammo.playerDamage == null) ammo.playerDamage = playerDamageFallback;
                if (!string.IsNullOrEmpty(ammo.casingSprite))
                    ammo.loadedCasingSprite = LoadConfigSprite(folder, ammo.casingSprite, $"ammoTypes[{i}].casingSprite");
                if (!string.IsNullOrEmpty(ammo.minigameCasingSprite))
                    ammo.loadedMinigameCasingSprite = LoadConfigSprite(folder, ammo.minigameCasingSprite, $"ammoTypes[{i}].minigameCasingSprite");
            }
            prop.normalTexture = LoadConfigTexture(folder, prop.normalTexturePath, "normalTexturePath");
            prop.rackedTexture = LoadConfigTexture(folder, prop.rackedTexturePath, "rackedTexturePath");

            if (null != prop.bulletsSpritesPath && 0 < prop.bulletsSpritesPath.Count())
            {
                prop.bulletsSprites = new Sprite[prop.bulletsSpritesPath.Count];
                for (int i = 0; i < prop.bulletsSpritesPath.Count; i++)
                {
                    if (!string.IsNullOrEmpty(prop.bulletsSpritesPath[i]))
                        prop.bulletsSprites[i] = LoadConfigSprite(folder, prop.bulletsSpritesPath[i], $"bulletsSpritesPath[{i}]");
                }
            }
            else
            {
                prop.bulletsSprites = new Sprite[prop.ammoTypes.Count + 2];
                prop.bulletsSprites[0] = bulletsSpritesFallback[0];
                prop.bulletsSprites[1] = bulletsSpritesFallback[1];
                for (int i = 2; i < prop.bulletsSprites.Length; i++)
                    prop.bulletsSprites[i] = bulletsSpritesFallback[2];
            }

            prop.magazineTextures = new Dictionary<string, Texture2D>();
            if (null != prop.magazineTexturesPath && 0 < prop.magazineTexturesPath.Count())
            {
                foreach (KeyValuePair<string, string> kvp in prop.magazineTexturesPath)
                    if (!string.IsNullOrEmpty(kvp.Value))
                        prop.magazineTextures.Add(kvp.Key, LoadConfigTexture(folder, kvp.Value, $"magazineTexturesPath['{kvp.Key}']"));
            }

            if (null != prop.fireModesSpritePath && 0 < prop.fireModesSpritePath.Count())
            {
                prop.fireModesSprite = new List<Sprite>();
                foreach (string spritePath in prop.fireModesSpritePath)
                    if (!string.IsNullOrEmpty(spritePath))
                        prop.fireModesSprite.Add(LoadConfigSprite(folder, spritePath, $"fireModesSpritePath[{prop.fireModesSprite.Count}]"));
                    else prop.fireModesSprite.Add(null);
            }
            else prop.fireModesSprite = fireModesFallback;

            if (!string.IsNullOrEmpty(prop.rackSpritePath))
                prop.rackSprite = LoadConfigSprite(folder, prop.rackSpritePath, "rackSpritePath");
            else prop.rackSprite = rackSpriteFallback;

            if (!string.IsNullOrEmpty(prop.unrackSpritePath))
                prop.unrackSprite = LoadConfigSprite(folder, prop.unrackSpritePath, "unrackSpritePath");
            else prop.unrackSprite = unrackSpriteFallback;

            if (!string.IsNullOrEmpty(prop.removeMagSpritePath))
                prop.removeMagSprite = LoadConfigSprite(folder, prop.removeMagSpritePath, "removeMagSpritePath");
            else prop.removeMagSprite = removeMagSpriteFallback;

            AudioLoader.LoadAudio(folder, prop.shootAudioPath, clip => prop.shootAudio = clip, shootAudioFallback, "shootAudioPath");
            AudioLoader.LoadAudio(folder, prop.unshootAudioPath, clip => prop.unshootAudio = clip, unshootAudioFallback, "unshootAudioPath");
            AudioLoader.LoadAudio(folder, prop.jamAudioPath, clip => prop.jamAudio = clip, jamAudioFallback, "jamAudioPath");
            AudioLoader.LoadAudio(folder, prop.rackAudioPath, clip => prop.rackAudio = clip, rackAudioFallback, "rackAudioPath");
            AudioLoader.LoadAudio(folder, prop.unrackAudioPath, clip => prop.unrackAudio = clip, unrackAudioFallback, "unrackAudioPath");
            AudioLoader.LoadAudio(folder, prop.loadRoundAudioPath, clip => prop.loadRoundAudio = clip, loadRoundAudioFallback, "loadRoundAudioPath");
            AudioLoader.LoadAudio(folder, prop.loadMagAudioPath, clip => prop.loadMagAudio = clip, loadMagAudioFallback, "loadMagAudioPath");
            AudioLoader.LoadAudio(folder, prop.removeMagAudioPath, clip => prop.removeMagAudio = clip, removeMagAudioFallback, "removeMagAudioPath");
            AudioLoader.LoadAudio(folder, prop.toggleFireModeAudioPath, clip => prop.toggleFireModeAudio = clip, toggleFireModeAudioFallback, "toggleFireModeAudioPath");

            if (null != prop.minigame)
            {
                var mainPaths = new[] {
                    prop.minigame.mainExperimentPath,
                    prop.minigame.mainMilkyTriggerOnPath,
                    prop.minigame.mainMilkyTriggerOffPath,
                    prop.minigame.mainDunePath
                };
                if (mainPaths.Any(string.IsNullOrEmpty))
                    throw new InvalidOperationException("Minigame requires mainExperimentPath, mainMilkyTriggerOnPath, mainMilkyTriggerOffPath and mainDunePath");
                string[] mainFields = { "mainExperimentPath", "mainMilkyTriggerOnPath", "mainMilkyTriggerOffPath", "mainDunePath" };
                // Shared paths intentionally reuse the same loaded sprite.
                var mainSprites = new Dictionary<string, Sprite>();
                foreach (string path in mainPaths)
                {
                    if (mainSprites.ContainsKey(path)) continue;
                    Sprite sprite = LoadConfigSprite(folder, path, "minigame." + mainFields[Array.IndexOf(mainPaths, path)]);
                    if (sprite == null)
                        throw new InvalidOperationException($"Could not load minigame main sprite '{path}'.");
                    mainSprites.Add(path, sprite);
                }
                prop.minigame.mainExperimentSprite = mainSprites[mainPaths[0]];
                prop.minigame.mainMilkyTriggerOnSprite = mainSprites[mainPaths[1]];
                prop.minigame.mainMilkyTriggerOffSprite = mainSprites[mainPaths[2]];
                prop.minigame.mainDuneSprite = mainSprites[mainPaths[3]];

                if (!string.IsNullOrEmpty(prop.minigame.sliderFrontPath))
                    prop.minigame.sliderFrontSprite = LoadConfigSprite(folder, prop.minigame.sliderFrontPath, "minigame.sliderFrontPath");
                else prop.minigame.sliderFrontSprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.sliderBackPath))
                    prop.minigame.sliderBackSprite = LoadConfigSprite(folder, prop.minigame.sliderBackPath, "minigame.sliderBackPath");
                else prop.minigame.sliderBackSprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.backLayerPath))
                    prop.minigame.backLayerSprite = LoadConfigSprite(folder, prop.minigame.backLayerPath, "minigame.backLayerPath");

                if (!string.IsNullOrEmpty(prop.minigame.rackedOnlyPath))
                    prop.minigame.rackedOnlySprite = LoadConfigSprite(folder, prop.minigame.rackedOnlyPath, "minigame.rackedOnlyPath");
                else prop.minigame.rackedOnlySprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.unrackedOnlyPath))
                    prop.minigame.unrackedOnlySprite = LoadConfigSprite(folder, prop.minigame.unrackedOnlyPath, "minigame.unrackedOnlyPath");
                else prop.minigame.unrackedOnlySprite = emptySprite;

                if (null == prop.minigame.ammos)
                    prop.minigame.ammos = new string[prop.ammoTypes.Count];

                if (null != prop.minigame.ammosInHandPaths && 0 != prop.minigame.ammosInHandPaths.Length)
                {
                    prop.minigame.ammosInHand = new Sprite[prop.minigame.ammosInHandPaths.Length];
                    for (int i = 0; i < prop.minigame.ammosInHandPaths.Length; i++)
                        prop.minigame.ammosInHand[i] = LoadConfigSprite(folder, prop.minigame.ammosInHandPaths[i], $"minigame.ammosInHandPaths[{i}]");
                }

                if (null != prop.minigame.ammosInPtrPaths && 0 != prop.minigame.ammosInPtrPaths.Length)
                {
                    prop.minigame.ammosInPtr = new Sprite[prop.minigame.ammosInPtrPaths.Length];
                    for (int i = 0; i < prop.minigame.ammosInPtrPaths.Length; i++)
                        prop.minigame.ammosInPtr[i] = LoadConfigSprite(folder, prop.minigame.ammosInPtrPaths[i], $"minigame.ammosInPtrPaths[{i}]");
                }

                if (!string.IsNullOrEmpty(prop.minigame.receiverPath))
                    prop.minigame.receiverTrigger = LoadConfigSprite(folder, prop.minigame.receiverPath, "minigame.receiverPath");
                else prop.minigame.receiverTrigger = emptySprite;

                for (int i = 0; i < prop.ammoTypes.Count; i++)
                {
                    prop.minigame.ammos[i] = prop.ammoTypes[i].round;
                }

                prop.minigame.ammosIcons = new Sprite[prop.ammoTypes.Count()];
                for (int i = 0; i < prop.ammoTypes.Count(); i++)
                    prop.minigame.ammosIcons[i] = prop.bulletsSprites[i + 2];

                if (!string.IsNullOrEmpty(prop.minigame.magReleasePath))
                    prop.minigame.magReleaseTrigger = LoadConfigSprite(folder, prop.minigame.magReleasePath, "minigame.magReleasePath");
                else prop.minigame.magReleaseTrigger = emptySprite;

                if (null != prop.minigame.magazinePaths)
                {
                    prop.minigame.magazineSprites = prop.minigame.magazinePaths.ToDictionary(
                        pair => pair.Key,
                        pair => LoadConfigSprite(folder, pair.Value, $"minigame.magazinePaths['{pair.Key}']")
                    );
                    prop.minigame.magazines = prop.minigame.magazineSprites.Keys.ToArray();
                }

                if (!string.IsNullOrEmpty(prop.minigame.magazineDragPath))
                    prop.minigame.magazineDragTrigger = LoadConfigSprite(folder, prop.minigame.magazineDragPath, "minigame.magazineDragPath");
                else prop.minigame.magazineDragTrigger = emptySprite;

                if (null != prop.minigame.casingsPath && 0 != prop.minigame.casingsPath.Length)
                {
                    prop.minigame.casingsSprite = new Sprite[prop.minigame.casingsPath.Length];
                    for (int i = 0; i < prop.minigame.casingsPath.Length; i++)
                        if (!string.IsNullOrEmpty(prop.minigame.casingsPath[i]))
                            prop.minigame.casingsSprite[i] = LoadConfigSprite(folder, prop.minigame.casingsPath[i], $"minigame.casingsPath[{i}]");
                }

                if (null != prop.minigame.ammosInMagazinePaths)
                    prop.minigame.ammosInMagazine = prop.minigame.ammosInMagazinePaths.ToDictionary(
                        pair => pair.Key,
                        pair => LoadConfigSpriteArray(folder, pair.Value, $"minigame.ammosInMagazinePaths['{pair.Key}']")
                    );

                if (!string.IsNullOrEmpty(prop.minigame.laserTogglePath))
                    prop.minigame.laserToggleTrigger = LoadConfigSprite(folder, prop.minigame.laserTogglePath, "minigame.laserTogglePath");
                else prop.minigame.laserToggleTrigger = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.laserPath))
                    prop.minigame.laserImage = LoadConfigSprite(folder, prop.minigame.laserPath, "minigame.laserPath");
                else prop.minigame.laserImage = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.fireTogglePath))
                    prop.minigame.fireToggleTrigger = LoadConfigSprite(folder, prop.minigame.fireTogglePath, "minigame.fireTogglePath");

                if (null != prop.minigame.fireModePaths)
                {
                    prop.minigame.fireModeSprites = new Sprite[prop.minigame.fireModePaths.Length];
                    for (int i = 0; i < prop.minigame.fireModePaths.Length; i++)
                        if (!string.IsNullOrEmpty(prop.minigame.fireModePaths[i]))
                            prop.minigame.fireModeSprites[i] = LoadConfigSprite(folder, prop.minigame.fireModePaths[i], $"minigame.fireModePaths[{i}]");
                }

                if (null != prop.minigame.coverAnimationPaths && 0 != prop.minigame.coverAnimationPaths.Length)
                {
                    prop.minigame.coverAnimation = new Sprite[prop.minigame.coverAnimationPaths.Length];
                    for (int i = 0; i < prop.minigame.coverAnimationPaths.Length; i++)
                        prop.minigame.coverAnimation[i] = LoadConfigSprite(folder, prop.minigame.coverAnimationPaths[i], $"minigame.coverAnimationPaths[{i}]");
                }
            }

            ItemInfo info = new ItemInfo
            {
                category = prop.category,
                slotRotation = prop.slotRotation,
                usable = true,
                usableOnLimb = false,
                destroyAtZeroCondition = false,
                combineable = false,
                weight = prop.weight,
                useAction = delegate(Body body, Item item)
                {
                    item.GetComponent<RshGun>().OnInventoryUse();
                },
                value = prop.value,
                rec = new Recognition(prop.recognition),
                tags = "cangetwet,gun",
                fullName = prop.fullName,
                description = prop.description,
                onlyHoldInHands = prop.onlyHoldInHands
            };
            info.SetTags();

            Plugin.RegisterGunCuCore(itemId, info, TextureToSprite(prop.normalTexture, prop.ppu), prop);

            if (prop.milkyCanSell)
                Plugin.milkyGuns.Add(itemId);

            if (!prop.craftable)
                return;

            Recipe recipe = new Recipe
            {
                INT = prop.craftInt,
                result = new RecipeResult
                {
                    id = itemId,
                },
                items = new List<RecipeItem> { },
                isRepair = false,
                category = Recipes.RecipeCategory.Utilities,
            };
            foreach (string i in prop.recipe)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    specificId = i,
                    specific = true,
                });
            }
            if (0f < prop.craftBiochem)
            {
                recipe.items.Add(new RecipeItem(prop.craftBiochem)
                {
                    isLiquid = true,
                    specificId = "biochem",
                    specific = true,
                    minimumCondition = prop.craftBiochem,
                });
            }
            if (0f < prop.craftHammering)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    quality = new CraftingQuality("hammering", prop.craftHammering),
                    destroyItem = false,
                });
            }
            if (0f < prop.craftCutting)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    quality = new CraftingQuality("cutting", prop.craftCutting),
                    destroyItem = false,
                });
            }
            Plugin.recipes.Add(recipe);
        }

        public static void LoadMags()
        {
            Plugin.milkyMags = new List<string>();
            LoadConfigs<MagJson>("Mags", "mag.json", LoadMag);
        }

        public static void LoadMag(string itemId, string folder, MagJson prop)
        {
            CreateValidation(folder).Magazine(prop);
            AudioLoader.LoadAudio(folder, prop.loadRoundAudioPath, clip => prop.loadRoundAudio = clip, loadRoundAudioFallback, "loadRoundAudioPath");
            AudioLoader.LoadAudio(folder, prop.unloadRoundAudioPath, clip => prop.unloadRoundAudio = clip, unloadRoundAudioFallback, "unloadRoundAudioPath");

            ItemInfo info = new ItemInfo
            {
                category = prop.category,
                slotRotation = prop.slotRotation,
                usable = !prop.pseudoMagazine,
                usableOnLimb = false,
                destroyAtZeroCondition = false,
                combineable = false,
                weight = prop.weight,
                scaleWeightWithCondition = true,
                useAction = delegate(Body body, Item item)
                {
                    item.GetComponent<RshMag>().OnInventoryUse(body);
                },
                value = prop.value,
                rec = new Recognition(prop.recognition),
                tags = "belttool",
                fullName = prop.fullName,
                description = prop.description,
            };
            info.SetTags();

            prop.emptySprite = LoadConfigSprite(folder, "item", "item sprite");
            if (!string.IsNullOrEmpty(prop.nonEmptySpritePath))
                prop.nonEmptySprite = LoadConfigSprite(folder, prop.nonEmptySpritePath, "nonEmptySpritePath");
            Plugin.RegisterMagCuCore(itemId, info, prop.emptySprite, prop);

            if (prop.milkyCanSell)
                Plugin.milkyMags.Add(itemId);

            if (!prop.craftable)
                return;

            Recipe recipe = new Recipe
            {
                INT = prop.craftInt,
                result = new RecipeResult
                {
                    id = itemId,
                    resultCondition = 0f,
                },
                items = new List<RecipeItem> { },
                isRepair = false,
                category = Recipes.RecipeCategory.Utilities,
            };
            foreach (string i in prop.recipe)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    specificId = i,
                    specific = true,
                });
            }
            if (0f < prop.craftBiochem)
            {
                recipe.items.Add(new RecipeItem(prop.craftBiochem)
                {
                    isLiquid = true,
                    specificId = "biochem",
                    specific = true,
                    minimumCondition = prop.craftBiochem,
                });
            }
            if (0f < prop.craftHammering)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    quality = new CraftingQuality("hammering", prop.craftHammering),
                    destroyItem = false,
                });
            }
            if (0f < prop.craftCutting)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    quality = new CraftingQuality("cutting", prop.craftCutting),
                    destroyItem = false,
                });
            }
            Plugin.recipes.Add(recipe);
        }

        public static void LoadRounds()
        {
            LoadConfigs<RoundJson>("Rounds", "round.json", LoadRound);
        }

        public static void LoadRound(string itemId, string folder, RoundJson prop)
        {
            CreateValidation(folder).Round(prop);
            ItemInfo info = new ItemInfo
            {
                category = prop.category,
                slotRotation = prop.slotRotation,
                usable = false,
                usableOnLimb = false,
                destroyAtZeroCondition = true,
                weight = prop.weight,
                value = prop.value,
                tags = "bullet",
                rec = new Recognition(prop.recognition),
                fullName = prop.fullName,
                description = prop.description,
            };
            info.SetTags();

            Plugin.RegisterRoundCuCore(itemId, info, LoadConfigSprite(folder, "item", "item sprite"));

            if (!prop.craftable)
                return;

            Recipe recipe = new Recipe
            {
                INT = prop.craftInt,
                result = new RecipeResult
                {
                    id = itemId,
                    amount = prop.craftAmount
                },
                items = new List<RecipeItem> { },
                isRepair = false,
                category = Recipes.RecipeCategory.Utilities,
            };
            foreach (string i in prop.recipe)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    specificId = i,
                    specific = true,
                });
            }
            if (0 < prop.craftHammering)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    quality = new CraftingQuality("hammering", prop.craftHammering),
                    destroyItem = false,
                });
            }
            if (0 < prop.craftCutting)
            {
                recipe.items.Add(new RecipeItem(0f)
                {
                    quality = new CraftingQuality("cutting", prop.craftCutting),
                    destroyItem = false,
                });
            }
            Plugin.recipes.Add(recipe);
        }

        private static T LoadConfigAsset<T>(string field, Func<T> load)
        {
            try { return load(); }
            catch (Exception ex)
            {
                throw new ConfigValidationException(new[] { $"{field}: {ex.GetType().Name}: {ex.Message}" }, ex);
            }
        }

        private static Sprite LoadConfigSprite(string folder, string path, string field)
            => LoadConfigAsset(field, () =>
            {
                if (string.IsNullOrWhiteSpace(path)) return null;
                Sprite sprite = LoadSprite(folder, path);
                if (sprite == null) throw new InvalidDataException($"Could not load sprite '{path}'.");
                return sprite;
            });

        private static Texture2D LoadConfigTexture(string folder, string path, string field)
            => LoadConfigAsset(field, () =>
            {
                Texture2D texture = LoadTexture(Path.Combine(folder, path));
                if (texture == null) throw new InvalidDataException($"Could not load texture '{path}'.");
                return texture;
            });

        private static Sprite[] LoadConfigSpriteArray(string folder, string[] paths, string field)
        {
            Sprite[] result = new Sprite[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                result[i] = LoadConfigSprite(folder, paths[i], $"{field}[{i}]");
            return result;
        }

        public static Sprite[] LoadSpriteArray(string baseFolder, string[] paths)
        {
            Sprite[] result = new Sprite[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                result[i] = LoadSprite(baseFolder, paths[i]);
            return result;
        }

        public static Sprite LoadSprite(string baseFolder, string path, float ppu = 8.0f)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (path.StartsWith("COPY__"))
            {
                if (sprites.TryGetValue(path, out Sprite sprite))
                    return sprite;
                throw new Exception($"[NewFirearms] {path} refer to non-existing asset! Did you got the loading order wrong?");
            }

            path = Path.Combine(baseFolder, path);
            Sprite result = TextureToSprite(LoadTexture(path), ppu);
            string cacheKey = "COPY__" + path.Substring(28).Replace("\\", "/");
            sprites[cacheKey] = result;
            return result;
        }

        public static Texture2D LoadTexture(string path)
        {
            path += ".png";
            if (!File.Exists(path))
            {
                UnityEngine.Debug.LogError($"[NewFirearms] {path} is expected, but doesn't exist!");
                return null;
            }

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(8, 8);
            if (!texture.LoadImage(fileData))
            {
                UnityEngine.Object.Destroy(texture);
                throw new InvalidDataException($"Could not decode PNG file '{path}'.");
            }
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        public static Sprite TextureToSprite(Texture2D texture, float ppu = 8.0f)
        {
            if (null == texture)
                return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
