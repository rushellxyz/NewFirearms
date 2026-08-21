using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    // settings for mod
    public class SettingsJson
    {
        public bool ignoreCourseLock = false;
        public bool strictSync = true;
        public float maxSliderValue = 4f;
        public float gunSyncRate = 0.5f;
        public float magSyncRate = 0.5f;
    }

    // params of gun model
    public class GunJson
    {
        public string fullName;
        public string description;
        public string category = "utility";
        public float slotRotation = -90f;
        public float weight;
        public int value;
        public int recognition;
        public bool milkyCanSell = true;
        public bool onlyHoldInHands;

        public bool craftable = false;
        public List<string> recipe;
        public float craftBiochem;
        public float craftCutting;
        public float craftHammering;
        public int craftInt;

        public int feedType;
        /*
        * 1 - Magazine feed
        * 2 - Tube
        * 3 - Barrel
        * 4 - Revolver
        */
        public int internalCapacity = 1;
        public List<AmmoInfo> ammoTypes;
        public float desiredGasTime;
        public List<byte> fireModes;
        /*
        * 0 - Safe
        * 1 - Pump action
        * 2 - Semi auto
        * 3 - Full auto
        */
        public List<string> magazineType;
        public float barrelOffset;

        public string normalTexturePath;
        public string rackedTexturePath;
        public string shootAudioPath;
        public string unshootAudioPath;
        public string jamAudioPath;
        public string rackAudioPath;
        public string unrackAudioPath;
        public string loadRoundAudioPath;
        public string loadMagAudioPath;
        public string removeMagAudioPath;
        public string toggleFireModeAudioPath;
        public string rackSpritePath;
        public string unrackSpritePath;
        public string removeMagSpritePath;
        public List<string> fireModesSpritePath;
        public List<string> bulletsSpritesPath;
        public Dictionary<string, string> magazineTexturesPath;

        public Texture2D normalTexture;
        public Texture2D rackedTexture;
        public AudioClip shootAudio;
        public AudioClip unshootAudio;
        public AudioClip jamAudio;
        public AudioClip rackAudio;
        public AudioClip unrackAudio;
        public AudioClip loadRoundAudio;
        public AudioClip loadMagAudio;
        public AudioClip removeMagAudio;
        public AudioClip toggleFireModeAudio;
        public Sprite rackSprite;
        public Sprite unrackSprite;
        public Sprite removeMagSprite;
        public List<Sprite> fireModesSprite;
        public Sprite[] bulletsSprites;
        public Dictionary<string, Texture2D> magazineTextures;

        public JsonMinigameInfo minigame;

        public float ppu = 8f;
    }

    public class JsonMinigameInfo : GunMinigame.MinigameInfo
    {
        public string mainPath;
        public string sliderFrontPath;
        public string sliderBackPath;
        public string rackedOnlyPath;
        public string unrackedOnlyPath;
        public string[] casingPaths;
        public string[] ammosInHandPaths;
        public string[] ammosInPtrPaths;
        public string receiverPath;
        public string magReleasePath;
        public Dictionary<string, string> magazinePaths;
    }

    // params of ammo shoot
    public class AmmoInfo
    {
        public string round;
        public float structureDamage;
        public float animalDamage;

        //public ProjectileParams projectileParams;
        public PlayerDamage playerDamage;
        public ExplosionParams explosionParams;

        public float knockback;
        public float muzzleRise;
        public int pellets = 1;
        public float verticalSpread;
        public float loudness;
        public float conditionLoss;
        public bool caseless = false;
        public bool ragdollRecoil;
    }

    public class PlayerDamage
    {
        public float traumaAmount;
        public float pain;
        public float adrenaline;
        public float skinDamage;
        public float muscleDamage;
        public float bleedAmount;
        public float wearableDamage;
        public float venomAmount; // i heard that prostethics dont get venom injected TODO test if getting shot into prostethic gives venom
        public float shrapnelChance;
        public float infectionChance;
        public float dislocationChance;
        public float fractureChance;
        public float internalBleed; // 0.085l/m per 10 internal, max 25
        public float brainDamage;
        public float disfigureChance;
        public float chloroformAmount;
    }

    /*public class ProjectileParams
    {
        public float speed;
        public float gravityScale;
        public float
    }*/

    // params of mag model
    public class MagJson
    {
        public string fullName;
        public string description;
        public string category = "utility";
        public float slotRotation = -90f;
        public float weight;
        public int value;
        public int recognition;
        public bool milkyCanSell = true;

        public bool craftable;
        public List<string> recipe;
        public float craftBiochem;
        public float craftCutting;
        public float craftHammering;
        public int craftInt;

        public int capacity;
        public List<string> magazineType;
        public List<string> ammoTypes;

        public string loadRoundAudioPath;
        public string unloadRoundAudioPath;

        public AudioClip loadRoundAudio;
        public AudioClip unloadRoundAudio;
    }

    // params of round model
    public class RoundJson
    {
        public string fullName;
        public string description;
        public string category = "utility";
        public float slotRotation = -0f;
        public float weight;
        public int value = 0;
        public int recognition;

        public bool craftable;
        public List<string> recipe;
        public float craftHammering;
        public float craftCutting;
        public int craftInt;
        public int craftAmount;
    }
}
