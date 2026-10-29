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

        public static void LoadGuns()
        {
            Plugin.milkyGuns = new List<string>();

            string[] folders = Directory.GetDirectories("BepInEx/plugins/NewFirearms/Guns/");
            Array.Sort(folders);
            foreach (string folder in folders)
            {
                string itemId = Path.GetFileName(folder);
                string jsonPath = Path.Combine(folder, "gun.json");
                if (!File.Exists(jsonPath))
                {
                    UnityEngine.Debug.Log($"{folder} exists but doesnt contain gun.json");
                    continue;
                }

                GunJson prop = null;
                try
                {
                    prop = JsonConvert.DeserializeObject<GunJson>(File.ReadAllText(jsonPath));
                }
                catch (JsonReaderException ex)
                {
                    UnityEngine.Debug.LogError($"[NewFirearms] Error parsing {jsonPath}: {ex.Message}");
                    continue;
                }

                try
                {
                    LoadGun(itemId, folder, prop);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[NewFirearms] Error loading {jsonPath}: {ex.Message}");
                }
            }
        }

        public static void LoadGun(string itemId, string folder, GunJson prop)
        {
            foreach (AmmoInfo ammo in prop.ammoTypes)
            {
                if (!string.IsNullOrEmpty(ammo.casingSprite))
                    ammo.loadedCasingSprite = LoadSprite(folder, ammo.casingSprite);
                if (!string.IsNullOrEmpty(ammo.minigameCasingSprite))
                    ammo.loadedMinigameCasingSprite = LoadSprite(folder, ammo.minigameCasingSprite);
            }
            prop.normalTexture = LoadTexture(Path.Combine(folder, prop.normalTexturePath));
            prop.rackedTexture = LoadTexture(Path.Combine(folder, prop.rackedTexturePath));

            if (null != prop.bulletsSpritesPath && 0 < prop.bulletsSpritesPath.Count())
            {
                prop.bulletsSprites = new Sprite[prop.bulletsSpritesPath.Count];
                for (int i = 0; i < prop.bulletsSpritesPath.Count; i++)
                {
                    if (!string.IsNullOrEmpty(prop.bulletsSpritesPath[i]))
                        prop.bulletsSprites[i] = LoadSprite(folder, prop.bulletsSpritesPath[i]);
                }
            }
            else prop.bulletsSprites = bulletsSpritesFallback;

            prop.magazineTextures = new Dictionary<string, Texture2D>();
            if (null != prop.magazineTexturesPath && 0 < prop.magazineTexturesPath.Count())
            {
                foreach (KeyValuePair<string, string> kvp in prop.magazineTexturesPath)
                    if (!string.IsNullOrEmpty(kvp.Value))
                        prop.magazineTextures.Add(kvp.Key, LoadTexture(Path.Combine(folder, kvp.Value)));
            }

            if (null != prop.fireModesSpritePath && 0 < prop.fireModesSpritePath.Count())
            {
                prop.fireModesSprite = new List<Sprite>();
                foreach (string spritePath in prop.fireModesSpritePath)
                    if (!string.IsNullOrEmpty(spritePath))
                        prop.fireModesSprite.Add(LoadSprite(folder, spritePath));
                    else prop.fireModesSprite.Add(null);
            }
            else prop.fireModesSprite = fireModesFallback;

            if (!string.IsNullOrEmpty(prop.rackSpritePath))
                prop.rackSprite = LoadSprite(folder, prop.rackSpritePath);
            else prop.rackSprite = rackSpriteFallback;

            if (!string.IsNullOrEmpty(prop.unrackSpritePath))
                prop.unrackSprite = LoadSprite(folder, prop.unrackSpritePath);
            else prop.unrackSprite = unrackSpriteFallback;

            if (!string.IsNullOrEmpty(prop.removeMagSpritePath))
                prop.removeMagSprite = LoadSprite(folder, prop.removeMagSpritePath);
            else prop.removeMagSprite = removeMagSpriteFallback;

            AudioLoader.LoadAudio(folder, prop.shootAudioPath, clip => prop.shootAudio = clip, shootAudioFallback);
            AudioLoader.LoadAudio(folder, prop.unshootAudioPath, clip => prop.unshootAudio = clip, unshootAudioFallback);
            AudioLoader.LoadAudio(folder, prop.jamAudioPath, clip => prop.jamAudio = clip, jamAudioFallback);
            AudioLoader.LoadAudio(folder, prop.rackAudioPath, clip => prop.rackAudio = clip, rackAudioFallback);
            AudioLoader.LoadAudio(folder, prop.unrackAudioPath, clip => prop.unrackAudio = clip, unrackAudioFallback);
            AudioLoader.LoadAudio(folder, prop.loadRoundAudioPath, clip => prop.loadRoundAudio = clip, loadRoundAudioFallback);
            AudioLoader.LoadAudio(folder, prop.loadMagAudioPath, clip => prop.loadMagAudio = clip, loadMagAudioFallback);
            AudioLoader.LoadAudio(folder, prop.removeMagAudioPath, clip => prop.removeMagAudio = clip, removeMagAudioFallback);
            AudioLoader.LoadAudio(folder, prop.toggleFireModeAudioPath, clip => prop.toggleFireModeAudio = clip, toggleFireModeAudioFallback);

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
                // Shared paths intentionally reuse the same loaded sprite.
                var mainSprites = new Dictionary<string, Sprite>();
                foreach (string path in mainPaths)
                {
                    if (mainSprites.ContainsKey(path)) continue;
                    Sprite sprite = LoadSprite(folder, path);
                    if (sprite == null)
                        throw new InvalidOperationException($"Could not load minigame main sprite '{path}'.");
                    mainSprites.Add(path, sprite);
                }
                prop.minigame.mainExperimentSprite = mainSprites[mainPaths[0]];
                prop.minigame.mainMilkyTriggerOnSprite = mainSprites[mainPaths[1]];
                prop.minigame.mainMilkyTriggerOffSprite = mainSprites[mainPaths[2]];
                prop.minigame.mainDuneSprite = mainSprites[mainPaths[3]];

                if (!string.IsNullOrEmpty(prop.minigame.sliderFrontPath))
                    prop.minigame.sliderFrontSprite = LoadSprite(folder, prop.minigame.sliderFrontPath);
                else prop.minigame.sliderFrontSprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.sliderBackPath))
                    prop.minigame.sliderBackSprite = LoadSprite(folder, prop.minigame.sliderBackPath);
                else prop.minigame.sliderBackSprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.backLayerPath))
                    prop.minigame.backLayerSprite = LoadSprite(folder, prop.minigame.backLayerPath);

                if (!string.IsNullOrEmpty(prop.minigame.rackedOnlyPath))
                    prop.minigame.rackedOnlySprite = LoadSprite(folder, prop.minigame.rackedOnlyPath);
                else prop.minigame.rackedOnlySprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.unrackedOnlyPath))
                    prop.minigame.unrackedOnlySprite = LoadSprite(folder, prop.minigame.unrackedOnlyPath);
                else prop.minigame.unrackedOnlySprite = emptySprite;

                if (null == prop.minigame.ammos)
                    prop.minigame.ammos = new string[prop.ammoTypes.Count];

                if (null != prop.minigame.ammosInHandPaths && 0 != prop.minigame.ammosInHandPaths.Length)
                {
                    prop.minigame.ammosInHand = new Sprite[prop.minigame.ammosInHandPaths.Length];
                    for (int i = 0; i < prop.minigame.ammosInHandPaths.Length; i++)
                        prop.minigame.ammosInHand[i] = LoadSprite(folder, prop.minigame.ammosInHandPaths[i]);
                }

                if (null != prop.minigame.ammosInPtrPaths && 0 != prop.minigame.ammosInPtrPaths.Length)
                {
                    prop.minigame.ammosInPtr = new Sprite[prop.minigame.ammosInPtrPaths.Length];
                    for (int i = 0; i < prop.minigame.ammosInPtrPaths.Length; i++)
                        prop.minigame.ammosInPtr[i] = LoadSprite(folder, prop.minigame.ammosInPtrPaths[i]);
                }

                if (!string.IsNullOrEmpty(prop.minigame.receiverPath))
                    prop.minigame.receiverTrigger = LoadSprite(folder, prop.minigame.receiverPath);
                else prop.minigame.receiverTrigger = emptySprite;

                for (int i = 0; i < prop.ammoTypes.Count; i++)
                {
                    if (null == prop.ammoTypes[i].playerDamage)
                        prop.ammoTypes[i].playerDamage = playerDamageFallback;
                    if (null != prop.minigame)
                        prop.minigame.ammos[i] = prop.ammoTypes[i].round;
                }

                prop.minigame.ammosIcons = new Sprite[prop.ammoTypes.Count()];
                for (int i = 0; i < prop.ammoTypes.Count(); i++)
                    prop.minigame.ammosIcons[i] = prop.bulletsSprites[i + 2];

                if (!string.IsNullOrEmpty(prop.minigame.magReleasePath))
                    prop.minigame.magReleaseTrigger = LoadSprite(folder, prop.minigame.magReleasePath);
                else prop.minigame.magReleaseTrigger = emptySprite;

                if (null != prop.minigame.magazinePaths)
                {
                    prop.minigame.magazineSprites = prop.minigame.magazinePaths.ToDictionary(
                        pair => pair.Key,
                        pair => LoadSprite(folder, pair.Value)
                    );
                    prop.minigame.magazines = prop.minigame.magazineSprites.Keys.ToArray();
                }

                if (!string.IsNullOrEmpty(prop.minigame.magazineDragPath))
                    prop.minigame.magazineDragTrigger = LoadSprite(folder, prop.minigame.magazineDragPath);
                else prop.minigame.magazineDragTrigger = emptySprite;

                if (null != prop.minigame.casingsPath && 0 != prop.minigame.casingsPath.Length)
                {
                    prop.minigame.casingsSprite = new Sprite[prop.minigame.casingsPath.Length];
                    for (int i = 0; i < prop.minigame.casingsPath.Length; i++)
                        if (!string.IsNullOrEmpty(prop.minigame.casingsPath[i]))
                            prop.minigame.casingsSprite[i] = LoadSprite(folder, prop.minigame.casingsPath[i]);
                }

                if (null != prop.minigame.ammosInMagazinePaths)
                    prop.minigame.ammosInMagazine = prop.minigame.ammosInMagazinePaths.ToDictionary(
                        pair => pair.Key,
                        pair => LoadSpriteArray(folder, pair.Value)
                    );

                if (!string.IsNullOrEmpty(prop.minigame.laserTogglePath))
                    prop.minigame.laserToggleTrigger = LoadSprite(folder, prop.minigame.laserTogglePath);
                else prop.minigame.laserToggleTrigger = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.laserPath))
                    prop.minigame.laserImage = LoadSprite(folder, prop.minigame.laserPath);
                else prop.minigame.laserImage = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.fireTogglePath))
                    prop.minigame.fireToggleTrigger = LoadSprite(folder, prop.minigame.fireTogglePath);

                if (null != prop.minigame.fireModePaths)
                {
                    prop.minigame.fireModeSprites = new Sprite[prop.minigame.fireModePaths.Length];
                    for (int i = 0; i < prop.minigame.fireModePaths.Length; i++)
                        if (!string.IsNullOrEmpty(prop.minigame.fireModePaths[i]))
                            prop.minigame.fireModeSprites[i] = LoadSprite(folder, prop.minigame.fireModePaths[i]);
                }

                if (null != prop.minigame.coverAnimationPaths && 0 != prop.minigame.coverAnimationPaths.Length)
                {
                    prop.minigame.coverAnimation = new Sprite[prop.minigame.coverAnimationPaths.Length];
                    for (int i = 0; i < prop.minigame.coverAnimationPaths.Length; i++)
                        prop.minigame.coverAnimation[i] = LoadSprite(folder, prop.minigame.coverAnimationPaths[i]);
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

            foreach (string folder in Directory.GetDirectories("BepInEx/plugins/NewFirearms/Mags/"))
            {
                string itemId = Path.GetFileName(folder);
                string jsonPath = Path.Combine(folder, "mag.json");
                if (!File.Exists(jsonPath))
                {
                    UnityEngine.Debug.Log($"{folder} exists but doesn't contain mag.json");
                    continue;
                }

                MagJson prop = null;
                try
                {
                    prop = JsonConvert.DeserializeObject<MagJson>(File.ReadAllText(jsonPath));
                }
                catch (JsonReaderException ex)
                {
                    UnityEngine.Debug.LogError($"Error parsing {jsonPath}: {ex.Message}");
                    continue;
                }

                AudioLoader.LoadAudio(folder, prop.loadRoundAudioPath, clip => prop.loadRoundAudio = clip, loadRoundAudioFallback);
                AudioLoader.LoadAudio(folder, prop.unloadRoundAudioPath, clip => prop.unloadRoundAudio = clip, unloadRoundAudioFallback);

                ItemInfo info = new ItemInfo
                {
                    category = prop.category,
                    slotRotation = prop.slotRotation,
                    usable = true,
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

                prop.emptySprite = LoadSprite(folder, "item");
                if (!string.IsNullOrEmpty(prop.nonEmptySpritePath))
                    prop.nonEmptySprite = LoadSprite(folder, prop.nonEmptySpritePath);
                Plugin.RegisterMagCuCore(itemId, info, prop.emptySprite, prop);

                if (prop.milkyCanSell)
                    Plugin.milkyMags.Add(itemId);

                if (!prop.craftable)
                    continue;

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
        }

        public static void LoadRounds()
        {
            foreach (string folder in Directory.GetDirectories("BepInEx/plugins/NewFirearms/Rounds/"))
            {
                string itemId = Path.GetFileName(folder);
                string jsonPath = Path.Combine(folder, "round.json");
                if (!File.Exists(jsonPath))
                {
                    UnityEngine.Debug.Log($"{folder} exists but doesn't contain round.json");
                    continue;
                }

                RoundJson prop = null;
                try
                {
                    prop = JsonConvert.DeserializeObject<RoundJson>(File.ReadAllText(jsonPath));
                }
                catch (JsonReaderException ex)
                {
                    UnityEngine.Debug.LogError($"Error parsing {jsonPath}: {ex.Message}");
                    continue;
                }

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

                Plugin.RegisterRoundCuCore(itemId, info, LoadSprite(folder, "item"));

                if (!prop.craftable)
                    continue;

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
            texture.LoadImage(fileData);
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
