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
    [BepInPlugin("com.rushellxyz.newfirearms", "New Firearms", "1.6.5")]
    [BepInDependency("com.rushellxyz.gunminigame", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("net.cucorelib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("meow.catpatch", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static SettingsJson sett;
        public static List<Recipe> recipes;
        public static List<string> milkyGuns;
        public static List<string> milkyMags;
        public static bool togetherMpEnabled;
        public static bool catPatchActive;

        // There because CUCoreLib overwrites CustomData on multiplayer clients. Блять, а нахуя

        // Runtime custom data can arrive after components on multiplayer clients.
        // Keep locally loaded definitions (including their Unity assets) available.
        internal static readonly Dictionary<string, GunJson> GunDefinitions = new Dictionary<string, GunJson>();
        internal static readonly Dictionary<string, MagJson> MagazineDefinitions = new Dictionary<string, MagJson>();

        void Awake()
        {
            if (!File.Exists("BepInEx/plugins/NewFirearms/Resources/bulletEmpty.png"))
            {
                Logger.LogFatal("NewFirearms/ is missing! Did you unpacked too deep?");
                return;
            }

            togetherMpEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("CasualtiesMP") && 0 == PlayerPrefs.GetInt("CasualtiesMP_FORCE_DISABLE_MP_MOD");
            catPatchActive = togetherMpEnabled && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("meow.catpatch");

            if (catPatchActive)
                Logger.LogInfo("Cat patch detected - cancelling some of our patches to prevent conflict (try to not kill yourself)");

            var harmony = new Harmony("com.rushellxyz.newfirearms");
            harmony.PatchAll();

            recipes = new List<Recipe>();

            ItemLoader.SetupResourcesDict();
            SetupGunminigame();
            LoadSettings();
            ItemLoader.LoadFallbackResources();
            ItemLoader.LoadGuns();
            ItemLoader.LoadMags();
            ItemLoader.LoadRounds();
            RegisterRunSettings();
            RegisterCourse();
            ItemLoader.ClearResourcesDict();

            if (togetherMpEnabled)
                MpOperations(harmony);
        }

        // """Fix""" cant load magazines into Thorn slinger
        void Start()
        {
            if (CUCoreLib.Registries.ItemRegistry.RegisteredItems.ContainsKey("thornslinger"))
                CUCoreLib.Registries.ItemRegistry.RegisteredItems["thornslinger"].description = ModLocale.Get("newfirearms.thornslinger.description");
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

        private static readonly HashSet<string> vanillaItems = new HashSet<string> { "boxof12gauge", "riflemagazine", "smallmagazine", "pistol", "shotgun", "rifle", "makeshiftrifle" };
        public static void RegisterGunCuCore(string id, ItemInfo info, Sprite sprite, GunJson prop)
        {
            GunDefinitions[id] = prop;
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
                VisualOffset = new Vector2(prop.itemSpriteXOffset, prop.itemSpriteYOffset),
                SpawnComponents = new List<string> { "NewFirearms.RshGun, NewFirearms.dll" }
            };
            if (prop.hasLaser)
                cuInfo.SpawnComponents.Add("NewFirearms.LaserPointer, NewFirearms.dll");
            cuInfo.SetTags();
            CUCoreLib.Registries.ItemRegistry.Register(id, cuInfo);
            if (vanillaItems.Contains(id))
                CCLWorkaround.itemsToDesc.Add(id, (info.description, info.fullName));
        }

        public static void RegisterMagCuCore(string id, ItemInfo info, Sprite sprite, MagJson prop)
        {
            MagazineDefinitions[id] = prop;
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
                SpawnComponents = new List<string> { "NewFirearms.RshMag, NewFirearms.dll" }
            };
            cuInfo.SetTags();
            CUCoreLib.Registries.ItemRegistry.Register(id, cuInfo);
        }

        public static void RegisterRoundCuCore(string id, ItemInfo info, Sprite sprite)
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

        void SetupGunminigame()
        {
            GunMinigame.MinigameManager.bulletToShow = (it) => {
                RshMag mag = it.GetComponent<RshMag>();
                if (null == mag)
                {
                    UnityEngine.Debug.LogWarning("[GunMinigame-NewFirearms] Expected item doesnt have a RshMag :tourniqet:");
                    return null;
                }
                if (mag.prop == null || mag.rounds == null || mag.rounds.Count == 0 || 0 > mag.rounds[0] || mag.rounds[0] >= mag.prop.ammoTypes.Count)
                    return null;
                return mag.prop.ammoTypes[mag.rounds[0]];
            };
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



    // CUCoreLib loads the embedded lang files; only the conditional description needs selecting.
    [HarmonyPatch(typeof(Locale), "LoadLanguage")]
    class LocalePatch
    {
        [HarmonyAfter("net.cucorelib")]
        static void Postfix()
        {
            if (Plugin.togetherMpEnabled && Locale.currentLang != null)
                Locale.currentLang.other["runsetnewfirearms.gundmgmultdsc"] =
                    ModLocale.Get("runsetnewfirearms.gundmgmultdsc.mp");
        }
    }

    // patch to add suicide command
    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    class ConsoleScriptPatch
    {
        // patch to add commands
        static void Postfix(ConsoleScript __instance)
        {
            ConsoleScript.Commands.Add(new Command("suicide", ModLocale.Get("newfirearms.command.suicide.description"), delegate(string[] args)
            {
                if (!PlayerCamera.main.body.FindByTagThorough("gun", out var it))
                {
                    UnityEngine.Debug.Log(ModLocale.Get("newfirearms.command.suicide.noGun"));
                    return;
                }
                if (!it.GetComponent<RshGun>().IsReady())
                {
                    UnityEngine.Debug.Log(ModLocale.Get("newfirearms.command.suicide.notReady"));
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
