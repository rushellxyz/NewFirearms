using BepInEx;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Currently, whenevar it creates new 9mmround/12gaugeround/556round the round would still have AmmoScript
// TODO Come up with way to destroy it, bc, optimization

namespace NewFirearms
{
    [BepInPlugin("com.rushellxyz.newfirearms", "New Firearms", "1.6.1")]
    [BepInDependency("com.rushellxyz.gunminigame", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.rushellxyz.rshlib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("meow.catpatch", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static SettingsJson sett;
        public static List<Recipe> recipes;
        public static List<string> milkyGuns;
        public static List<string> milkyMags;
        public AudioClip shootAudioFallback;
        public AudioClip unshootAudioFallback;
        public AudioClip jamAudioFallback;
        public AudioClip rackAudioFallback;
        public AudioClip unrackAudioFallback;
        public AudioClip loadRoundAudioFallback;
        public AudioClip loadMagAudioFallback;
        public AudioClip removeMagAudioFallback;
        public AudioClip toggleFireModeAudioFallback;
        public AudioClip unloadRoundAudioFallback;
        public Sprite rackSpriteFallback;
        public Sprite unrackSpriteFallback;
        public Sprite removeMagSpriteFallback;
        public static List<Sprite> fireModesFallback;
        public static Sprite[] bulletsSpritesFallback;
        public static PlayerDamage playerDamageFallback;
        public static Sprite emptySprite;

        public static bool useRshLib;
        public static bool useCuCore;
        public static bool togetherMpEnabled;
        public static bool catPatchActive;

        void Awake()
        {
            togetherMpEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("CasualtiesMP") && 0 == PlayerPrefs.GetInt("CasualtiesMP_FORCE_DISABLE_MP_MOD");
            useRshLib = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rushellxyz.rshlib");
            useCuCore = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("net.cucorelib");
            catPatchActive = togetherMpEnabled && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("meow.catpatch");
            if (useRshLib && useCuCore)
            {
                Logger.LogFatal("RshLib and CuCoreLib both are installed. They conflict! Choose one.");
                return;
            }
       else if (!useRshLib && !useCuCore)
            {
                Logger.LogFatal("RshLib and CuCoreLib both are missing. Install RshLib!");
                return;
            }

            if (catPatchActive)
                Logger.LogInfo("Cat patch detected - cancelling some of our patches to prevent conflict (try to not kill yourself)");

            var harmony = new Harmony("com.rushellxyz.newfirearms");
            harmony.PatchAll();

            recipes = new List<Recipe>();

            SetupResourcesDict();
            LoadSettings();
            LoadFallbackResources();
            LoadGuns();
            LoadMags();
            LoadRounds();
            RegisterRunSettings();
            RegisterCourse();
            ClearResourcesDict();

            if (togetherMpEnabled)
                MpOperations(harmony);
        }

        void PatchPostfix(Harmony harmony, string targetClass, string targetMethod, string postfixClass)
        {
            var target = AccessTools.Method(AccessTools.TypeByName(targetClass), targetMethod);
            var postfix = AccessTools.Method(System.Type.GetType(postfixClass), "Postfix");
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        void PatchPrefix(Harmony harmony, string targetClass, string targetMethod, string prefixClass)
        {
            var target = AccessTools.Method(AccessTools.TypeByName(targetClass), targetMethod);
            var prefix = AccessTools.Method(System.Type.GetType(prefixClass), "Prefix");
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }

        void LoadSettings()
        {
            try
            {
                sett = JsonConvert.DeserializeObject<SettingsJson>(File.ReadAllText("BepInEx/plugins/NewFirearms/settings.json"));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error parsing settings!:\n{e}\nSettings would fallback to default.");
                sett = new SettingsJson();
            }
        }

        void LoadFallbackResources()
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
            { // why is new List<Sprite>(4) doesnt work?
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

        static Dictionary<string, Sprite> sprites;
        void SetupResourcesDict()
        {
            sprites = new Dictionary<string, Sprite>();
        }

        void ClearResourcesDict()
        {
            sprites = null;
        }

        void LoadGuns()
        {
            milkyGuns = new List<string>();

            // On my linux btrfs Directory.GetDirectories gives out folder in alphabetic order
            // but as c# documentation states it is not guarnteed to happend on other filesystems
            string[] a = Directory.GetDirectories("BepInEx/plugins/NewFirearms/Guns/");
            Array.Sort(a);
            foreach (string folder in a)
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
                    continue;
                }
            }
        }

        void LoadGun(string itemId, string folder, GunJson prop)
        {
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
       else     prop.bulletsSprites = bulletsSpritesFallback;
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
               else     prop.fireModesSprite.Add(null);
            }
       else     prop.fireModesSprite = fireModesFallback;
            if (!string.IsNullOrEmpty(prop.rackSpritePath))
                prop.rackSprite = LoadSprite(folder, prop.rackSpritePath);
       else     prop.rackSprite = rackSpriteFallback;
            if (!string.IsNullOrEmpty(prop.unrackSpritePath))
                prop.unrackSprite = LoadSprite(folder, prop.unrackSpritePath);
       else     prop.unrackSprite = unrackSpriteFallback;
            if (!string.IsNullOrEmpty(prop.removeMagSpritePath))
                prop.removeMagSprite = LoadSprite(folder, prop.removeMagSpritePath);
       else     prop.removeMagSprite = removeMagSpriteFallback;
            if (!string.IsNullOrEmpty(prop.shootAudioPath))
                prop.shootAudio = LoadWav(Path.Combine(folder, prop.shootAudioPath));
       else     prop.shootAudio = shootAudioFallback;
            if (!string.IsNullOrEmpty(prop.unshootAudioPath))
                prop.unshootAudio = LoadWav(Path.Combine(folder, prop.unshootAudioPath));
       else     prop.unshootAudio = unshootAudioFallback;
            if (!string.IsNullOrEmpty(prop.jamAudioPath))
                prop.jamAudio = LoadWav(Path.Combine(folder, prop.jamAudioPath));
       else     prop.jamAudio = jamAudioFallback;
            if (!string.IsNullOrEmpty(prop.rackAudioPath))
                prop.rackAudio = LoadWav(Path.Combine(folder, prop.rackAudioPath));
       else     prop.rackAudio = rackAudioFallback;
            if (!string.IsNullOrEmpty(prop.unrackAudioPath))
                prop.unrackAudio = LoadWav(Path.Combine(folder, prop.unrackAudioPath));
       else     prop.unrackAudio = unrackAudioFallback;
            if (!string.IsNullOrEmpty(prop.loadRoundAudioPath))
                prop.loadRoundAudio = LoadWav(Path.Combine(folder, prop.loadRoundAudioPath));
       else     prop.loadRoundAudio = loadRoundAudioFallback;
            if (!string.IsNullOrEmpty(prop.loadMagAudioPath))
                prop.loadMagAudio = LoadWav(Path.Combine(folder, prop.loadMagAudioPath));
       else     prop.loadMagAudio = loadMagAudioFallback;
            if (!string.IsNullOrEmpty(prop.removeMagAudioPath))
                prop.removeMagAudio = LoadWav(Path.Combine(folder, prop.removeMagAudioPath));
       else     prop.removeMagAudio = removeMagAudioFallback;
            if (!string.IsNullOrEmpty(prop.toggleFireModeAudioPath))
                prop.toggleFireModeAudio = LoadWav(Path.Combine(folder, prop.toggleFireModeAudioPath));
       else     prop.toggleFireModeAudio = toggleFireModeAudioFallback;

            if (null != prop.minigame)
            {
                if (!string.IsNullOrEmpty(prop.minigame.mainPath))
                    prop.minigame.mainSprite = LoadSprite(folder, prop.minigame.mainPath);
           else     UnityEngine.Debug.LogWarning($"[NewFirearms] {folder} is decalared that its minigame compatabile, but missing mandatory mainPath field!");

                if (!string.IsNullOrEmpty(prop.minigame.sliderFrontPath))
                    prop.minigame.sliderFrontSprite = LoadSprite(folder, prop.minigame.sliderFrontPath);
           else     prop.minigame.sliderFrontSprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.sliderBackPath))
                    prop.minigame.sliderBackSprite = LoadSprite(folder, prop.minigame.sliderBackPath);
           else     prop.minigame.sliderBackSprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.rackedOnlyPath))
                    prop.minigame.rackedOnlySprite = LoadSprite(folder, prop.minigame.rackedOnlyPath);
           else     prop.minigame.rackedOnlySprite = emptySprite;

                if (!string.IsNullOrEmpty(prop.minigame.unrackedOnlyPath))
                    prop.minigame.unrackedOnlySprite = LoadSprite(folder, prop.minigame.unrackedOnlyPath);
           else     prop.minigame.unrackedOnlySprite = emptySprite;

                if (null != prop.minigame.casingPaths && 0 != prop.minigame.casingPaths.Length)
                {
                    prop.minigame.casingSprites = new Sprite[prop.minigame.casingPaths.Length];
                    for (int i = 0; i < prop.minigame.casingPaths.Length; i++)
                        prop.minigame.casingSprites[i] = LoadSprite(folder, prop.minigame.casingPaths[i]);
                }

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
           else     prop.minigame.receiverTrigger = emptySprite;

                for (int i = 0; i < prop.ammoTypes.Count; i++)
                {
                    if (null == prop.ammoTypes[i].playerDamage)
                        prop.ammoTypes[i].playerDamage = playerDamageFallback;
                    if (null != prop.minigame)
                        prop.minigame.ammos[i] = prop.ammoTypes[i].round;
                }

                prop.minigame.ammosIcons =  new Sprite[prop.ammoTypes.Count()];
                for (int i = 0; i < prop.ammoTypes.Count(); i++)
                    prop.minigame.ammosIcons[i] = prop.bulletsSprites[i+2];

                if (!string.IsNullOrEmpty(prop.minigame.magReleasePath))
                    prop.minigame.magReleaseTrigger = LoadSprite(folder, prop.minigame.magReleasePath);
           else     prop.minigame.magReleaseTrigger = emptySprite;

                if (null != prop.minigame.magazinePaths)
                {
                    prop.minigame.magazineSprites = prop.minigame.magazinePaths.ToDictionary(
                        pair => pair.Key,
                        pair => LoadSprite(folder, pair.Value)
                    );
                    prop.minigame.magazines = prop.minigame.magazineSprites.Keys.ToArray();
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

            if (useRshLib)
                RegisterGunRshLib(itemId, info, TextureToSprite(prop.normalTexture, prop.ppu), prop);
       else     RegisterGunCuCore(itemId, info, TextureToSprite(prop.normalTexture, prop.ppu), prop);

            if (prop.milkyCanSell)
                milkyGuns.Add(itemId);

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
            recipes.Add(recipe);
        }

        void LoadMags()
        {
            milkyMags = new List<string>();

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

                prop.loadRoundAudio = loadRoundAudioFallback;
                prop.unloadRoundAudio = unloadRoundAudioFallback;

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
                        item.GetComponent<RshMag>().RemoveRound(body);
                    },
                    value = prop.value,
                    rec = new Recognition(prop.recognition),
                    tags = "belttool",
                    fullName = prop.fullName,
                    description = prop.description,
                };
                info.SetTags();

                if (useRshLib)
                    RegisterMagRshLib(itemId, info, LoadSprite("BepInEx/plugins/NewFirearms/", $"Mags/{itemId}/item"), prop);
           else     RegisterMagCuCore(itemId, info, LoadSprite("BepInEx/plugins/NewFirearms/", $"Mags/{itemId}/item"), prop);

                if (prop.milkyCanSell)
                    milkyMags.Add(itemId);

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
                    recipe.items.Add(new RecipeItem(0f)
                    {
                        specificId = "biochem",
                        isLiquid = true,
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
                recipes.Add(recipe);
            }
        }

        void LoadRounds()
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

                if (useRshLib)
                    RegisterRoundRshLib(itemId, info, LoadSprite(folder, "item"));
           else     RegisterRoundCuCore(itemId, info, LoadSprite(folder, "item"));

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
                recipes.Add(recipe);
            }
        }

        void RegisterRunSettings()
        {
            for (int i = RunSettings.settingTypes.Count - 1; i >= 0; i--)
            {
                if ("encumbrancecap" != RunSettings.settingTypes[i].name)
                    continue;
                RunSettings.settingTypes.Insert(i + 1, new RunSettingFloat("newfirearms.gundmgmult")
                {
                    limits = new RangeF(0f, Plugin.sett.maxSliderValue),
                    postfix = "x",
                });
                RunSettings.settingTypes.Insert(i + 1, new RunSettingFloat("newfirearms.stunhitbox")
                {
                    limits = new RangeF(1f, Plugin.sett.maxSliderValue + 1f),
                    postfix = "x",
                });
                RunSettings.settingTypes.Insert(i + 1, new RunSettingBool("newfirearms.gunjamming"));
                RunSettings.settingTypes.Insert(i + 1, new RunSettingBool("newfirearms.gunejectcasing"));
                break;
            }
            RunSettings.presets[0].presetValues.Add("newfirearms.gunjamming", true);
            RunSettings.presets[0].presetValues.Add("newfirearms.gundmgmult", 1f);
            RunSettings.presets[0].presetValues.Add("newfirearms.stunhitbox", 2.5f);
            RunSettings.presets[0].presetValues.Add("newfirearms.gunejectcasing", true);
            RunSettings.presets[1].presetValues.Add("newfirearms.gunjamming", false);
            RunSettings.presets[1].presetValues.Add("newfirearms.gundmgmult", 1.2f);
            RunSettings.presets[1].presetValues.Add("newfirearms.stunhitbox", 3f);
            RunSettings.presets[1].presetValues.Add("newfirearms.gunejectcasing", true);
            RunSettings.presets[2].presetValues.Add("newfirearms.gunjamming", false);
            RunSettings.presets[2].presetValues.Add("newfirearms.gundmgmult", 1.5f);
            RunSettings.presets[2].presetValues.Add("newfirearms.stunhitbox", 4f);
            RunSettings.presets[2].presetValues.Add("newfirearms.gunejectcasing", true);
            RunSettings.presets[3].presetValues.Add("newfirearms.gunjamming", true);
            RunSettings.presets[3].presetValues.Add("newfirearms.gundmgmult", 0.8f);
            RunSettings.presets[3].presetValues.Add("newfirearms.stunhitbox", 1.5f);
            RunSettings.presets[3].presetValues.Add("newfirearms.gunejectcasing", true);
            RunSettings.presets[4].presetValues.Add("newfirearms.gunjamming", true);
            RunSettings.presets[4].presetValues.Add("newfirearms.gundmgmult", 1f);
            RunSettings.presets[4].presetValues.Add("newfirearms.stunhitbox", 2.5f);
            RunSettings.presets[4].presetValues.Add("newfirearms.gunejectcasing", true);
        }

        void RegisterGunRshLib(string id, ItemInfo info, Sprite sprite, GunJson prop)
        {
            RshLib.Plugin.RegisterItem(id, new RshLib.RshItem
            {
                info = info,
                sprite = sprite,
                onSpawn = delegate(GameObject go, string extraData)
                {
                    RshGun rshGun = go.AddComponent<RshGun>();
                    rshGun.prop = prop;
                },
            });
        }

        void RegisterMagRshLib(string id, ItemInfo info, Sprite sprite, MagJson prop)
        {
            RshLib.Plugin.RegisterItem(id, new RshLib.RshItem
            {
                info = info,
                sprite = sprite,
                onSpawn = delegate(GameObject go, string data)
                {
                    RshMag rshMag = go.AddComponent<RshMag>();
                    rshMag.prop = prop;

                    if (string.IsNullOrEmpty(data))
                        return;
                    sbyte ammoIndex = 0;
                    if (!sbyte.TryParse(data, out ammoIndex))
                    {
                        ammoIndex = (sbyte)prop.ammoTypes.IndexOf(data);
                        if (-1 >= ammoIndex)
                        {
                            UnityEngine.Debug.LogError($"Ammo {data} doesn't exist or can't be fit into this magazine");
                            return;
                        }
                    }
                    rshMag.Fill(ammoIndex, prop.capacity);
                    rshMag.existed = true;
                },
            });
        }

        void RegisterRoundRshLib(string id, ItemInfo info, Sprite sprite)
        {
            RshLib.Plugin.RegisterItem(id, new RshLib.RshItem
            {
                info = info,
                sprite = sprite,
            });
        }

        void RegisterGunCuCore(string id, ItemInfo info, Sprite sprite, GunJson prop)
        {
            CUCoreLib.Data.CustomItemInfo cuInfo = new CUCoreLib.Data.CustomItemInfo
            {
                category = info.category,
                slotRotation = info.slotRotation,
                usable = info.usable,
                usableOnLimb = info.usableOnLimb,
                destroyAtZeroCondition = info.destroyAtZeroCondition,
                combineable = info.combineable,
                weight = info.weight,
                useAction = info.useAction,
                value = info.value,
                rec = info.rec,
                tags = info.tags,
                fullName = info.fullName,
                description = info.description,
                onlyHoldInHands = info.onlyHoldInHands,

                Icon = sprite,
                SpawnComponents = new List<string> { "NewFirearms.RshGun, NewFirearms.dll" },
                CustomData = new Dictionary<string, object> { ["prop"] = prop }
            };
            cuInfo.SetTags();
            CUCoreLib.Registries.ItemRegistry.Register(id, cuInfo);
        }

        void RegisterMagCuCore(string id, ItemInfo info, Sprite sprite, MagJson prop)
        {
            CUCoreLib.Data.CustomItemInfo cuInfo = new CUCoreLib.Data.CustomItemInfo
            {
                category = info.category,
                slotRotation = info.slotRotation,
                usable = info.usable,
                usableOnLimb = info.usableOnLimb,
                destroyAtZeroCondition = info.destroyAtZeroCondition,
                combineable = info.combineable,
                weight = info.weight,
                scaleWeightWithCondition = info.scaleWeightWithCondition,
                useAction = info.useAction,
                value = info.value,
                rec = info.rec,
                tags = info.tags,
                fullName = info.fullName,
                description = info.description,

                Icon = sprite,
                SpawnComponents = new List<string> { "NewFirearms.RshMag, NewFirearms.dll" },
                CustomData = new Dictionary<string, object> { ["prop"] = prop }
            };
            cuInfo.SetTags();
            CUCoreLib.Registries.ItemRegistry.Register(id, cuInfo);
        }

        void RegisterRoundCuCore(string id, ItemInfo info, Sprite sprite)
        {
            CUCoreLib.Registries.ItemRegistry.Register(id, info, sprite);
        }

        void RegisterCourse()
        {
            int layer = 3;
            if (Plugin.sett.ignoreCourseLock)
                layer = 0;
            TutorialHandler.availableCourses.Insert(3, ("newfirearms.firearmhandlingcourse", layer, typeof(FirearmHandlingCourse)));
        }

        void MpOperations(Harmony harmony)
        {
            if (!catPatchActive)
                PatchPrefix(harmony, "Together.YOU_SHOULD_KILL_YOURSELF_NOOOW", "Client_SelfHarmMinigameMinigameEnd", "NewFirearms.YOU_SHOULD_KILL_YOURSELF_NOOOWPatch");
            Together.Multiplayer.RegisterCustomServerReceiver(SelfHarmerPatch.MSGID_SUICIDE_REQUEST, YOU_SHOULD_KILL_YOURSELF_NOOOWPatch.ReciveGunSuicideRequest);
            Together.Multiplayer.RegisterCustomClientReceiver(RshGun.MSGID_SYNC, RshGun.ClientSync);
            Together.Multiplayer.RegisterCustomServerReceiver(RshGun.MSGID_ACTION, RshGun.ServerReceiver);
            Together.Multiplayer.RegisterCustomClientReceiver(RshGun.MSGID_SHOOT_VISUALS, RshGun.ReceiveShoot);
            Together.Multiplayer.RegisterCustomClientReceiver(RshMag.MSGID_SYNC, RshMag.ClientSync);
            Together.Multiplayer.RegisterCustomServerReceiver(RshMag.MSGID_ACTION, RshMag.ServerReceiver);
        }

        public static Sprite LoadSprite(string baseFolder, string path, float ppu=8.0f)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (path.StartsWith("COPY__"))
            {
                if (sprites.TryGetValue(path, out Sprite sprite))
                    return sprite;
           else     throw new Exception($"[NewFirearms] {path} refer to non-existing asset! Did you got the loading order wrong?");
            }
            path = Path.Combine(baseFolder, path);
            Sprite result = TextureToSprite(LoadTexture(path), ppu);
            path = "COPY__" + path.Substring(28).Replace("\\", "/");//Replace("BepInEx/plugin/NewFirearms/", "");
            sprites[path] = result;// backslash sucks
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

        public static Sprite TextureToSprite(Texture2D texture, float ppu=8.0f)
        {
            if (null == texture)
                return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
        }

        //  BEGIN CODE BY ARTIFICAL INTELIGENCE
        public static Texture2D CombineTextures(List<Texture2D> textures)
        {
            if (textures == null || textures.Count == 0)
            {
                Debug.LogError("Texture list is empty or null.");
                return null;
            }

            // Use the first texture to define dimensions
            int width = textures[0].width;
            int height = textures[0].height;

            // Create the destination texture
            Texture2D resultTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Initialize the result array with transparency
            Color[] resultPixels = new Color[width * height];
            for (int i = 0; i < resultPixels.Count(); i++)
            {
                resultPixels[i] = Color.clear;
            }

            // Process each texture sequentially
            foreach (Texture2D tex in textures)
            {
                if (tex == null) continue;

                if (tex.width != width || tex.height != height)
                {
                    Debug.LogWarning($"Texture '{tex.name}' size mismatch. Skipping.");
                    continue;
                }

                Color[] currentPixels = tex.GetPixels();

                for (int i = 0; i < currentPixels.Count(); i++)
                {
                    Color srcColor = currentPixels[i];

                    if (srcColor != Color.magenta)
                    {
                        resultPixels[i] = srcColor;
                    }
                }
            }

            // Apply changes to the hardware texture memory
            resultTexture.SetPixels(resultPixels);
            resultTexture.Apply();
            resultTexture.filterMode = FilterMode.Point;

            return resultTexture;
        }

        public static AudioClip LoadWav(string absolutePath)
        {
            absolutePath += ".wav";
            if (!File.Exists(absolutePath))
            {
                UnityEngine.Debug.LogError($"{absolutePath} is expected, but doesn't exist!");
                return null;
            }

            byte[] fileBytes = File.ReadAllBytes(absolutePath);

            // WAV Header parsing constants
            int channels = BitConverter.ToInt16(fileBytes, 22);
            int frequency = BitConverter.ToInt32(fileBytes, 24);
            int pos = 12;

            // Locate data chunk
            while (pos < fileBytes.Length - 4)
            {
                if (fileBytes[pos] == 'd' && fileBytes[pos + 1] == 'a' && fileBytes[pos + 2] == 't' && fileBytes[pos + 3] == 'a')
                {
                    pos += 4;
                    break;
                }
                pos++;
            }

            int subChunk2Size = BitConverter.ToInt32(fileBytes, pos);
            pos += 4;

            // Convert 16-bit bytes to float samples
            int sampleCount = subChunk2Size / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = BitConverter.ToInt16(fileBytes, pos + i * 2) / 32768f;
            }

            // Create the clip instantly
            AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(absolutePath), sampleCount / channels, channels, frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }
        //   END  CODE BY ARTIFICAL INTELIGENCE

        // [1,2,3,4] > [2,3,4,1]
        public static void ShiftLeft<T>(ref List<T> list)
        {
            if (list == null || list.Count <= 1)
                return;

            T firstItem = list[0];
            for (int i = 0; i < list.Count - 1; i++)
                list[i] = list[i + 1];

            list[list.Count - 1] = firstItem;
        }

        // [1,2,3,4] > [4,1,2,3]
        public static void ShiftRight<T>(ref List<T> list)
        {
            if (list == null || list.Count <= 1)
                return;

            T lastItem = list[list.Count - 1];
            for (int i = list.Count - 1; i > 0; i--)
                list[i] = list[i - 1];

            list[0] = lastItem;
        }

        public static bool IsHost()
        {
            return Together.Net.IsServer;
        }

        public static bool IsDedicated()
        {
            return Together.Net.IsServer && Together.Net.IsDedicatedServer;
        }
    }

    // patch to add recipes
    [HarmonyPatch(typeof(Recipes), "SetUpRecipes")]
    class RecipesPatch
    {
        static readonly HashSet<string> VanillaRecipesToRemove = new HashSet<string>
        {
            "makeshiftrifle",
            "smallmagazine",
            "riflemagazine",
            "boxof12gauge",
        };

        static void Postfix()
        {
            // The replacment is neccesary to not break crafting for clients without the mod
            int recipeToAdd = 0;
            for (int i = Recipes.recipes.Count - 1; i >= 0; i--)
            {
                string result = Recipes.recipes[i].result.id;
                if (!VanillaRecipesToRemove.Contains(result))
                    continue;
                if (recipeToAdd >= Plugin.recipes.Count)
                {
                    UnityEngine.Debug.LogError("[NewFirearms] Unable to patch recipes! No replacment left.");
                    return;
                }
                UnityEngine.Debug.Log($"[NewFirearms] Replace recipe at {i} for {Plugin.recipes[recipeToAdd].result.id}");
                Plugin.recipes[recipeToAdd].hasMadeBefore = false;
                Plugin.recipes[recipeToAdd].specialKnown = false;
                Plugin.recipes[recipeToAdd].index = i;
                Recipes.recipes[i] = Plugin.recipes[recipeToAdd];
                recipeToAdd += 1;
            }
            for (; recipeToAdd < Plugin.recipes.Count; recipeToAdd++)
            {
                UnityEngine.Debug.Log($"[NewFirearms] Added recipe at {Recipes.recipes.Count} for {Plugin.recipes[recipeToAdd].result.id}");
                Plugin.recipes[recipeToAdd].hasMadeBefore = false;
                Plugin.recipes[recipeToAdd].specialKnown = false;
                Plugin.recipes[recipeToAdd].index = Recipes.recipes.Count;
                Recipes.recipes.Add(Plugin.recipes[recipeToAdd]);
            }
        }
    }



    // patch to add locale
    [HarmonyPatch(typeof(Locale), "LoadLanguage")]
    class LocalePatch
    {
        static void Postfix()
        {
            Locale.currentLang.other.Add("runsetnewfirearms.gunjamming", "Gun jaminng");
            Locale.currentLang.other.Add("runsetnewfirearms.gunjammingdsc", "Do guns jam?");
            Locale.currentLang.other.Add("runsetnewfirearms.gundmgmult", "Gun damage multiplier");
            if (Plugin.togetherMpEnabled)
                Locale.currentLang.other.Add("runsetnewfirearms.gundmgmultdsc", "How much damage guns deal to tiles, enemies and other players? Gets futrher multiplier by PVPDamageMultiplier rule when hitting another player");
       else     Locale.currentLang.other.Add("runsetnewfirearms.gundmgmultdsc", "How much damage guns deal to tiles and enemies?");
            Locale.currentLang.other.Add("runsetnewfirearms.gunejectcasing", "Gun eject casings");
            Locale.currentLang.other.Add("runsetnewfirearms.gunejectcasingdsc", "Do guns create casings after fire?");
            Locale.currentLang.other.Add("runsetnewfirearms.stunhitbox", "Stun hitbox size");
            Locale.currentLang.other.Add("runsetnewfirearms.stunhitboxdsc", "Whenevar you shoot right over/under enemy, they would still get stunned. Determines size of stun hitbox");

            Locale.currentLang.other.Add("newfirearms.firearmhandlingcourse", "Firearm handling course");
            Locale.currentLang.other.Add("newfirearms.firearmhandlingcourselock", "Reach layer 4 to unlock");
        }
    }

    // patch to add suicide command
    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    class ConsoleScriptPatch
    {
        // patch to add commands
        static void Postfix(ConsoleScript __instance)
        {
            ConsoleScript.Commands.Add(new Command("suicide", "You must have a loaded gun. Prevents last stand", delegate(string[] args)
            {
                if (!PlayerCamera.main.body.FindByTagThorough("gun", out var it))
                {
                    UnityEngine.Debug.Log("you dont have a gun. you are gonna suffer.");
                    return;
                }
                if (!it.GetComponent<RshGun>().IsReady())
                {
                    UnityEngine.Debug.Log("Gun is not ready. Try remove safety?");
                    return;
                }
                if (!Plugin.togetherMpEnabled || Plugin.IsHost())
                {
                    float oldHappiness = PlayerCamera.main.body.happiness;
                    PlayerCamera.main.body.happiness = -100000f;
                    PlayerCamera.main.body.harmer.AttemptHarm();
                    PlayerCamera.main.body.happiness = oldHappiness;
                }
           else {
                    SelfHarmerPatch.RequestGunSuicide();
                }
            }, new Dictionary<int, List<string>> {} ));
        }
    }
}
