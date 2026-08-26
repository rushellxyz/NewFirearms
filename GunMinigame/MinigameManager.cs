// I swear, i usually code better that this

/*
 RushellXYZ [EXPI],  — 17:12
 Типа фнкция если коллайдер кнопки и коллайдер руки пересекаются и машка нажата

 NEVERLITD — 17:14
 да, типо того

 мышка/курсор, это всего лишь направляющая для руки с коллайдером, которая при нажатии ЛКМ, активирует коллайдер у руки, что бы "рука"/коллайдер умела брать/хватать что либо, а при отжатии ЛКМ, отпусать/бросать, что либо
 NEVERLITD — 17:15
 тупым не техническим языком (моим): мышка которая на руке XD
 следит
 картинка руки с коллайдером ------> мышка/курсор

 ЛКМ               принимает запрос             если                    стыкуется                        да                      подбирает/хватает
 мышка/курсор ------> картинка руки с коллайдером -----> колайдер/хит-бокс предмета  -----> картинка руки с коллайдером
*/

using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

namespace GunMinigame
{
    public class MinigameInfo
    {
        public Sprite mainSprite;
        public Sprite sliderFrontSprite;
        public Sprite sliderBackSprite;
        public Sprite rackedOnlySprite;
        public Sprite unrackedOnlySprite;
        public float sliderMinimumPosition;
        public float sliderMaximumPosition;
        public float unrackedIdlePostion;
        public float rackPoint;
        public float rackedIdlePosition;
        public Sprite[] casingSprites;
        public float casingDelay;
        public string[] ammos;
        public Sprite[] ammosInHand;
        public Sprite[] ammosInPtr;
        public Sprite receiverTrigger;
        public Sprite[] ammosIcons;
        public float sliderHandFollowOffset;
        public bool canUseFannyPack;
        public Sprite magReleaseTrigger;
        public Dictionary<string, Sprite> magazineSprites;
        public string[] magazines;
        public float magazineXPosition;
        public float magazineYPosition;
        public float magazineXSpeed;
        public float magazineYSpeed;
        public float magazineAnimationTime;
        public float magazinesOffset;
        public int maximumMagazines;

        // should never be declared in json
        public ushort dCurrentAmmo;
        public bool initialized;
    }

    public interface IMinigameGun
    {
        public bool IsRacked();
        public void Rack();
        public bool DragOnto(Item item); // Returns whenever load is success
        public void RemoveMag();
        public string CurrentMag();
    }

    public class MinigameManager : MonoBehaviour
    {
        public bool active;

        public bool holdsSlide;
        public Vector2 onBeginHoldSlide;

        public Image handImage;
        public bool handIsHovered;
        public bool handFaded;
        public Image uiImage;

        public Image sliderFrontImage;
        public Image sliderBackImage;

        public Image casingAnim;
        private ushort casingIndex;
        private float casingTimer;

        public float sliderHoldOffset;
        public GameObject canvas;
        public GameObject uiBase;
        public GameObject gunBase;
        public GameObject maskBase;
        public Image mainImage;

        public static Sprite backgroundSprite;

        public RectTransform handTransform;
        public float xInertia;
        public float rotInertia;

        public static Body body => PlayerCamera.main.body;
        public Vector2 handVelocity;
        public Vector2 handPos;

        public static MinigameManager instance;

        public MinigameInfo info;
        public IMinigameGun gun;

        public bool shouldResetHandSprite;

        private float sliderLastXPos;

        public Image rackedSpecific;

        public Image receiverTrigger;

        public static Sprite bandolierBackground;
        public static Sprite bandolierForeground;
        public static Sprite ptrBlank;
        public Image bandolierBase;
        public Image bandolierFront;
        public Transform ptrBase;
        public Image[] ptrs;

        public Container bandolier;
        public Item holding;
        public GameObject holdingInMinigame;
        public bool ignoreNextMouseKey;

        public static Sprite hideSprite;
        public static Sprite showSprite;
        public Image hideShowButton;
        public bool hiding;
        public bool hiden;
        public float windowAlpha;
        public int ammoInBand;

        public bool shouldntRefreshBandolierCount;

        public static Sprite ammoSelectorSprite;
        public Image ammoSelector;
        public Image ammoIcon;

        public bool lastWindowAlphaWasLimit;

        public static Sprite ammoSelectWindowSprite;
        public static Sprite ammoSelectCursorSprite;
        public Image ammoSelectWindow;

        public List<Image> ammoSelectAmmo;
        public Image ammoSelectCursor;


        public static Sprite fannyPackSprite;
        public GameObject fannyPack;
        public Image fannyPackImage;

        public static Sprite fannyPackZipSprite;
        public Image fannyPackZip;
        public float fannyPackZipInertia;

        public Image magReleaseTrigger;

        public bool handIsInBandolier;

        public float magSyncIgnoreTime;
        public Image insertedMagazine;
        public string insertedMagazineType;

        public bool shouldUpdateMagazineCount;

        private List<Image> placedMagazines;


        bool spinningBandol;

        public static MinigameManager GetOrAddInstance()
        {
            if (null == instance)
                instance = PlayerCamera.main.gameObject.AddComponent<MinigameManager>();
            return instance;
        }

        public void AddRecoil(float knockback, float muzzleRise)
        {
            if (!active)
                return;

            if (null == uiBase)
            {
                UnityEngine.Debug.LogError("[NewFirearms] Attempt to draw recoil before minigame initialization!");
                return;
            }

            xInertia -= knockback * 3f;
            muzzleRise = Mathf.Abs(muzzleRise);
            rotInertia += muzzleRise * 2f;
            if (fannyPack.gameObject.activeSelf)
                fannyPackZipInertia += muzzleRise;
        }

        public void Hide()
        {
            PlayerCamera.main.gunMenu.SetActive(value: false);
            PlayerCamera.main.gunCrosshair.gameObject.SetActive(value: false);
            active = false;
            if (null != uiBase)
                uiBase.SetActive(false);
        }

        public void Show(MinigameInfo newInfo, IMinigameGun newGgun)
        {
            if (null == uiBase)
                PrepareGunUi();

            info = newInfo;
            if (!info.initialized)
            {
                info.sliderMaximumPosition *= Screen.width;
                info.sliderMinimumPosition *= Screen.width;
                info.unrackedIdlePostion *= Screen.width;
                info.rackedIdlePosition *= Screen.width;
                info.magazineXPosition *= Screen.width;
                info.magazineYPosition *= Screen.height;
                info.rackPoint *= Screen.width;
                info.magazineXSpeed *= Screen.width;
                info.magazineYSpeed *= Screen.height;
                info.initialized = true;
            }
            mainImage.sprite = info.mainSprite;
            sliderFrontImage.sprite = info.sliderFrontSprite;
            sliderBackImage.sprite = info.sliderBackSprite;
            receiverTrigger.sprite = info.receiverTrigger;
            magReleaseTrigger.sprite = info.magReleaseTrigger;
            /*            if (it.Stats.rec.recognizable)
             *             {                     * *
             *
        }*/
            ammoIcon.sprite = info.ammosIcons[info.dCurrentAmmo];
            EnsureAmmoIcons(info.ammos.Length);
            for (int i = 0; i < ammoSelectAmmo.Count; i++)
            {
                if (i < info.ammosIcons.Length)
                {
                    ammoSelectAmmo[i].sprite = info.ammosIcons[i];
                    ammoSelectAmmo[i].enabled = true;
                }
                else    ammoSelectAmmo[i].enabled = false;
            }
            active = true;
            gun = newGgun;

            uiBase.SetActive(true);
        }

        void PrepareGunUi()
        {
            if (null == backgroundSprite)
                PrepareResources();
            if (null == canvas)
                canvas = GameObject.Find("Canvas");
            uiBase = new GameObject("GunMinigame");
            uiBase.transform.SetParent(canvas.transform);
            uiBase.transform.localPosition = new Vector3(0f, -415f, 0f);

            hideShowButton = new GameObject("MinigameHideShowButton").AddComponent<Image>();
            hideShowButton.transform.SetParent(uiBase.transform);
            hideShowButton.transform.localPosition = new Vector3(0f, (187f/1440f)*Screen.height, 0f);
            hideShowButton.sprite = hideSprite;
            hideShowButton.GetComponent<RectTransform>().sizeDelta = new Vector3((150f/2560f)*Screen.width, (42.5f/1440f)*Screen.height);
            hideShowButton.gameObject.AddComponent<Button>().onClick.AddListener(()=>HideShow());
            hideShowButton.gameObject.AddComponent<ImageHoverror>();

            Vector3 size = new Vector3((670f/2560f)*Screen.width, (335f/1440f)*Screen.height);

            maskBase = new GameObject("MaskBase");
            maskBase.transform.SetParent(uiBase.transform);
            maskBase.transform.localPosition = Vector3.zero;
            maskBase.transform.localScale = Vector3.one;
            uiImage = maskBase.AddComponent<Image>();
            uiImage.sprite = backgroundSprite;
            uiImage.GetComponent<RectTransform>().sizeDelta = size;
            maskBase.AddComponent<ImageHoverror>();
            maskBase.AddComponent<RectMask2D>();

            gunBase = new GameObject("GunBase");
            gunBase.transform.SetParent(maskBase.transform);
            gunBase.transform.localPosition = Vector3.zero;
            gunBase.transform.localScale = Vector3.one;

            sliderBackImage = new GameObject("MinigameSliderBack").AddComponent<Image>();
            sliderBackImage.transform.SetParent(gunBase.transform);
            sliderBackImage.transform.localPosition = Vector3.zero;
            sliderBackImage.transform.localScale = Vector3.one;
            sliderBackImage.GetComponent<RectTransform>().sizeDelta = size;

            mainImage = new GameObject("MinigameMainSprite").AddComponent<Image>();
            mainImage.transform.SetParent(gunBase.transform);
            mainImage.transform.localPosition = Vector3.zero;
            mainImage.transform.localScale = Vector3.one;
            mainImage.GetComponent<RectTransform>().sizeDelta = size;

            rackedSpecific = new GameObject("MinigameRackedSpecific").AddComponent<Image>();
            rackedSpecific.transform.SetParent(gunBase.transform);
            rackedSpecific.transform.localPosition = Vector3.zero;
            rackedSpecific.transform.localScale = Vector3.one;
            rackedSpecific.GetComponent<RectTransform>().sizeDelta = size;

            casingAnim = new GameObject("MinigameCasingAnim").AddComponent<Image>();
            casingAnim.transform.SetParent(gunBase.transform);
            casingAnim.transform.localPosition = Vector3.zero;
            casingAnim.transform.localScale = Vector3.one;
            casingAnim.GetComponent<RectTransform>().sizeDelta = size;
            casingAnim.enabled = false;

            sliderFrontImage = new GameObject("MinigameSliderFront").AddComponent<Image>();
            sliderFrontImage.transform.SetParent(gunBase.transform);
            sliderFrontImage.transform.localPosition = Vector3.zero;
            sliderFrontImage.transform.localScale = Vector3.one;
            sliderFrontImage.alphaHitTestMinimumThreshold = 0.1f;
            sliderFrontImage.GetComponent<RectTransform>().sizeDelta = size;
            sliderFrontImage.gameObject.AddComponent<AlphaRaycastFilter>();
            EventTrigger sliderTrigger = sliderFrontImage.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry sliderDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            sliderDownEntry.callback.AddListener((data) => { SliderClickDown(); });
            sliderTrigger.triggers.Add(sliderDownEntry);

            receiverTrigger = new GameObject("MinigameReceiverTrigger").AddComponent<Image>();
            receiverTrigger.transform.SetParent(gunBase.transform);
            receiverTrigger.transform.localPosition = Vector3.zero;
            receiverTrigger.transform.localScale = Vector3.one;
            receiverTrigger.alphaHitTestMinimumThreshold = 0.1f;
            receiverTrigger.GetComponent<RectTransform>().sizeDelta = size;
            receiverTrigger.gameObject.AddComponent<AlphaRaycastFilter>();
            EventTrigger receiverTriggerTrigger = receiverTrigger.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry receiverTriggerEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drop };
            receiverTriggerEntry.callback.AddListener((data) => { ReceiverDrop(); });
            receiverTriggerTrigger.triggers.Add(receiverTriggerEntry);
            receiverTrigger.color = new Color(1f, 1f, 1f, 0f);
            receiverTrigger.gameObject.SetActive(false);

            magReleaseTrigger = new GameObject("MinigameMagRelease").AddComponent<Image>();
            magReleaseTrigger.transform.SetParent(gunBase.transform);
            magReleaseTrigger.transform.localPosition = Vector3.zero;
            magReleaseTrigger.transform.localScale = Vector3.one;
            magReleaseTrigger.alphaHitTestMinimumThreshold = 0.1f;
            magReleaseTrigger.GetComponent<RectTransform>().sizeDelta = size;
            magReleaseTrigger.gameObject.AddComponent<AlphaRaycastFilter>();
            magReleaseTrigger.gameObject.AddComponent<Button>().onClick.AddListener(() => MagRemoveButton());
            magReleaseTrigger.color = new Color(1f, 1f, 1f, 0f);

            fannyPack = new GameObject("MinigameFannyPack");
            fannyPack.transform.SetParent(maskBase.transform);
            fannyPack.transform.localPosition = Vector3.zero;
            fannyPack.transform.localScale = Vector3.one;

            fannyPackImage = new GameObject("MinigameFannyPackImage").AddComponent<Image>();
            fannyPackImage.transform.SetParent(fannyPack.transform);
            fannyPackImage.transform.localScale = Vector3.one;
            fannyPackImage.transform.localPosition = Vector3.zero;
            fannyPackImage.GetComponent<RectTransform>().sizeDelta = size;
            fannyPackImage.sprite = fannyPackSprite;
            fannyPackImage.raycastTarget = false;

            fannyPackZip = new GameObject("MinigameFannyPackZip").AddComponent<Image>();
            fannyPackZip.transform.SetParent(fannyPack.transform);
            fannyPackZip.transform.localPosition = new Vector3((75f/2560f)*Screen.width, (-60f/1440f)*Screen.height);
            fannyPackZip.transform.localScale = Vector3.one;
            fannyPackZip.sprite = fannyPackZipSprite;
            fannyPackZip.raycastTarget = false;
            fannyPackZip.GetComponent<RectTransform>().sizeDelta = new Vector3((20f/2560f)*Screen.width, (86f/1440f)*Screen.height);

            bandolierBase = new GameObject("MingameBandolierBase").AddComponent<Image>();
            bandolierBase.transform.SetParent(maskBase.transform);
            bandolierBase.transform.localPosition = Vector3.zero;
            bandolierBase.transform.localScale = Vector3.one;//new Vector3((1f/2560f)*Screen.width, (1f/1440f)*Screen.height);
            bandolierBase.sprite = bandolierBackground;
            bandolierBase.GetComponent<RectTransform>().sizeDelta = size;
            bandolierBase.gameObject.AddComponent<AlphaRaycastFilter>();
            bandolierBase.alphaHitTestMinimumThreshold = 0.1f;
            EventTrigger bandolierTrigger = bandolierBase.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry bandolierDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            EventTrigger.Entry bandolierExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            EventTrigger.Entry bandolierEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            bandolierDownEntry.callback.AddListener((data) => { BandolierClickDown(); });
            bandolierExitEntry.callback.AddListener((data) => { BandolierExitPtr(); });
            bandolierEnterEntry.callback.AddListener((data) => { BandolierEnterPtr(); });
            bandolierTrigger.triggers.Add(bandolierDownEntry);
            bandolierTrigger.triggers.Add(bandolierExitEntry);
            bandolierTrigger.triggers.Add(bandolierEnterEntry);

            ptrBase = new GameObject("MinigamePtrBase").transform;
            ptrBase.SetParent(bandolierBase.transform);
            ptrBase.localPosition = Vector3.zero;
            ptrBase.localScale = Vector3.one;

            ptrs = new Image[24];
            for (int i = 0; i < 24; i++)
            {
                Image ptr = new GameObject("MinigamePtr").AddComponent<Image>();
                ptr.transform.SetParent(ptrBase);
                ptr.transform.localPosition = new Vector3(((-330f/2560f)*Screen.width) + (i * ((30f/2560f)*Screen.width)), (-125f/1440f)*Screen.height);
                ptr.transform.localScale = new Vector3((1.25f/2560f)*Screen.width, (1.25f/1440f)*Screen.height, 0f);
                ptr.sprite = ptrBlank;
                ptr.GetComponent<RectTransform>().sizeDelta = new Vector2(26f, 63f);
                ptr.raycastTarget = false;
                ptrs[i] = ptr;
            }

            bandolierFront = new GameObject("MinigameBandolierForeground").AddComponent<Image>();
            bandolierFront.transform.SetParent(bandolierBase.transform);
            bandolierFront.transform.localPosition = Vector3.zero;
            bandolierFront.transform.localScale = Vector3.one;
            bandolierFront.sprite = bandolierForeground;
            bandolierFront.GetComponent<RectTransform>().sizeDelta = size;
            bandolierFront.raycastTarget = false;

            ammoSelector = new GameObject("MinigameAmmoSelector").AddComponent<Image>();
            ammoSelector.transform.SetParent(uiBase.transform);
            ammoSelector.transform.localScale = new Vector3((1f/2560f)*Screen.width, (1f/1440f)*Screen.height);
            ammoSelector.transform.localPosition = new Vector3((385f/2560f)*Screen.width, (-115f/1440f)*Screen.height);
            ammoSelector.sprite = ammoSelectorSprite;
            ammoSelector.gameObject.AddComponent<Button>().onClick.AddListener(()=>ToggleAmmo());
            ammoSelector.gameObject.AddComponent<ImageHoverror>();

            ammoIcon = new GameObject("MinigameAmmoIcon").AddComponent<Image>();
            ammoIcon.transform.SetParent(ammoSelector.transform);
            ammoIcon.transform.localScale = new Vector3((1f/3f), 0.5f);
            ammoIcon.transform.localPosition = Vector3.zero;
            ammoIcon.sprite = null;
            ammoIcon.raycastTarget = false;

            ammoSelectWindow = new GameObject("MinigameAmmoSelectWindow").AddComponent<Image>();
            ammoSelectWindow.transform.SetParent(uiBase.transform);
            ammoSelectWindow.transform.localScale = new Vector3((3f/2560f)*Screen.width, (1f/1440f)*Screen.height, 1f);
            ammoSelectWindow.transform.localPosition = new Vector3((590f/2560f)*Screen.width, (-115f/1440f)*Screen.height, 0);;
            ammoSelectWindow.sprite = ammoSelectWindowSprite;

            ammoSelectCursor = new GameObject("MinigameAmmoSelectCursor").AddComponent<Image>();
            ammoSelectCursor.transform.SetParent(ammoSelectWindow.transform);
            ammoSelectCursor.transform.localScale = new Vector3(0.8f*(1f/3f), 0.8f);
            ammoSelectCursor.transform.localPosition = new Vector3(-32.5f, 1f, 0f);
            ammoSelectCursor.sprite = ammoSelectCursorSprite;

            ammoSelectAmmo = new List<Image>();
            ammoSelectAmmo.Add(new GameObject("MinigameAmmoSelectAmmo-1").AddComponent<Image>());
            ammoSelectAmmo[0].transform.SetParent(ammoSelectWindow.transform);
            ammoSelectAmmo[0].transform.localPosition = new Vector3(-32.5f, 1f);
            ammoSelectAmmo[0].transform.localScale = new Vector3(0.5f*(1f/5f), 0.5f);
            ammoSelectAmmo[0].enabled = false;
            ammoSelectAmmo[0].gameObject.AddComponent<Button>().onClick.AddListener(() => SetAmmoType(0));

            handTransform = UnityEngine.Object.Instantiate(MinigameBase.main.handTransform.gameObject, Vector2.zero, Quaternion.identity).GetComponent<RectTransform>();
            handTransform.SetParent(maskBase.transform);
            handTransform.localScale = new Vector3((0.4f/2560f)*Screen.width, (0.4f/1440f)*Screen.height);
            handImage = handTransform.GetComponent<Image>();

            placedMagazines = new List<Image>();
        }

        void EnsureAmmoIcons(int count)
        {
            for (int i = count - ammoSelectAmmo.Count; i > 0; i--)
                AddAmmoIcon();
        }

        void AddAmmoIcon()
        {
            ushort curCount = (ushort)ammoSelectAmmo.Count;
            ammoSelectAmmo.Add(new GameObject($"MinigameAmmoSelectAmmo{curCount}").AddComponent<Image>());
            ammoSelectAmmo[curCount].transform.SetParent(ammoSelectWindow.transform);
            ammoSelectAmmo[curCount].transform.localScale = new Vector3(0.5f*(1f/5f), 0.5f);
            ammoSelectAmmo[curCount].transform.localPosition = new Vector3(ammoSelectAmmo[ammoSelectAmmo.Count - 2].transform.localPosition.x + 20 , 1f);
            ammoSelectAmmo[curCount].enabled = false;
            ammoSelectAmmo[curCount].gameObject.AddComponent<Button>().onClick.AddListener(() => SetAmmoType(curCount));
        }

        static void PrepareResources()
        {
            backgroundSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameBackground.png");
            bandolierBackground = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameBandolierBackground.png");
            bandolierForeground = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameBandolierForeground.png");
            ptrBlank = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigamePtr.png");
            if (null == hideSprite)
            {
                hideSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameHide.png");
                showSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameShow.png");
            }
            ammoSelectorSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameAmmoSelector.png");
            ammoSelectWindowSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameAmmoSelectWindow.png");
            ammoSelectCursorSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameAmmoSelectCursor.png");
            fannyPackSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameFannyPack.png");
            fannyPackZipSprite = Plugin.LoadSprite("BepInEx/plugins/NewFirearms/Resources/minigameFannyPackZip.png");
        }

        public Vector2 GetMousePos()
        {
            return (Vector2)Input.mousePosition +
            new Vector2(Mathf.PerlinNoise(Time.time * 2.9f, Time.time * 2.7f) - 0.5f, Mathf.PerlinNoise(Time.time * 2.86f, Time.time * 2.2f) - 0.5f)
            * body.averagePain * Mathf.Clamp01(1f - body.skills.RESFrom10 * 0.06f) * WorldGeneration.GetRunSettingFloat("minigamehandshake");
        }

        public void Update()
        {
            if (!active)
            {
                shouldntRefreshBandolierCount = false;
                shouldUpdateMagazineCount = true;
                handIsHovered = false;
                holdsSlide = false;
                shouldResetHandSprite = true;
                casingIndex = ushort.MaxValue;
                xInertia = 0f;
                rotInertia = 0f;
                return;
            }
            Item it = PlayerCamera.main.body.GetItem(PlayerCamera.main.body.handSlot);
            HandleInertia();

            Vector2 mousePos = GetMousePos();

            if (IsAnyGameUIOpen())
            {
                handFaded = true;
                shouldResetHandSprite = true;
                windowAlpha = Mathf.Clamp01(windowAlpha - Time.deltaTime * 4f);
            }
            else {
                if (handFaded && handIsHovered)
                {
                    shouldResetHandSprite = true;
                    shouldUpdateMagazineCount = true;
                    handPos = mousePos;
                }
                handFaded = !handIsHovered;
                windowAlpha = Mathf.Clamp01(windowAlpha + Time.deltaTime * 4f);
            }

            HandleMag();
            HandleCasings();
            HandleBandolier();
            HandleWindowAlpha();
            HandleFannyPack();

            if (handFaded)
            { // idk why that weird looking number for alpha, i stole it from decompiled code
                handImage.color = new Color(1f, 1f - body.averagePain * 0.01f, 1f - body.averagePain * 0.01f, Mathf.Clamp(handImage.color.a - Time.deltaTime * 4f, 0f, 0.8235294f));
                if (0f >= handImage.color.a)
                {
                    shouldntRefreshBandolierCount = false;
                    shouldResetHandSprite=true;
                    holdsSlide = false;
                    goto SliderHandle;
                }
            }
       else {
                handImage.color = new Color(1f, 1f - body.averagePain * 0.01f, 1f - body.averagePain * 0.01f, Mathf.Clamp(handImage.color.a + Time.deltaTime * 4f, 0f, 0.8235294f));
                var precachingcahce = KeyBinds.GetBind("attack");
                if (Input.GetKeyDown(precachingcahce) && !ignoreNextMouseKey)
                {
                    handImage.sprite = MinigameBase.main.handSprites[2];
                }
           else if (Input.GetKeyUp(precachingcahce) || shouldResetHandSprite)
                {
                    receiverTrigger.gameObject.SetActive(false);
                    shouldResetHandSprite = false;
                    holdsSlide = false;
                    if (null != holding)
                    {
                        if (!Plugin.IsMarksman(PlayerCamera.main.body) && !handIsInBandolier)
                        {
                            SpinBandolied();
                            holding.transform.parent.GetComponent<Container>().UnloadItem(holding);

                        }
                    else    shouldntRefreshBandolierCount=false;
                        holding = null;
                        UnityEngine.Object.Destroy(holdingInMinigame);
                    }
                    else if (null != holdingInMinigame)
                    {
                        ptrs[1].sprite = info.ammosInPtr[info.dCurrentAmmo];
                        UnityEngine.Object.Destroy(holdingInMinigame);
                    };
                    handImage.sprite = MinigameBase.main.handSprites[UnityEngine.Random.Range(0, 2)];
                }
                ignoreNextMouseKey = false;
            }

            float num2 = 0.25f;
            float num3 = 4f + body.skills.STRFrom10 * 0.3f;
            float num4 = 1.5f;
            num3 *= body.consciousness * 0.01f;
            num2 *= body.consciousness * 0.01f;
            Vector2 b = Vector2.ClampMagnitude((mousePos - handPos) * num2, 75f);
            handVelocity = Vector2.Lerp(handVelocity, b, num3 * Time.deltaTime);
            handVelocity = Vector2.Lerp(handVelocity, Vector2.zero, Time.deltaTime * num4);
            handVelocity *= 0.9f; // is this is fps-dependent, i just stole it from keypadminigame
            handPos += handVelocity * Time.deltaTime * 120f;

            // handPos = Vector2.Lerp(Minigame.game.handPos, mousePos, Time.deltaTime * 10f);

            float xPos;

            handTransform.position = handPos;
            SliderHandle:
            bool racked = gun.IsRacked();
            if (racked)
                rackedSpecific.sprite = info.rackedOnlySprite;
       else     rackedSpecific.sprite = info.unrackedOnlySprite;
            if (holdsSlide)
            {
                xPos = (handPos.x - onBeginHoldSlide.x) + sliderHoldOffset;
                if (xPos < info.sliderMinimumPosition)
                    xPos = info.sliderMinimumPosition;
                else if (xPos > info.sliderMaximumPosition)
                    xPos = info.sliderMaximumPosition;
                if ((!racked && xPos < info.rackPoint) || (racked && xPos > info.rackPoint))
                    gun.Rack();
                sliderFrontImage.transform.localPosition = new Vector3(xPos, 0f, 0f);
                sliderBackImage.transform.localPosition = new Vector3(xPos, 0f, 0f);
                xInertia += (xPos - sliderLastXPos) * 0.5f;
            }
       else {
                if (gun.IsRacked())
                    xPos = info.rackedIdlePosition;
           else     xPos = info.unrackedIdlePostion;
                sliderFrontImage.transform.localPosition = new Vector3(xPos, 0f, 0f);
                sliderBackImage.transform.localPosition = new Vector3(xPos, 0f, 0f);
            }
            sliderLastXPos = xPos;
        }

        private void HandleInertia()
        {
            gunBase.transform.localPosition += new Vector3(xInertia * Time.deltaTime * 32f, 0f);
            gunBase.transform.Rotate(0f, 0f, rotInertia * Time.deltaTime * 16f);

            float offset = 0f;
            if (holdsSlide && handIsHovered)
                offset = info.sliderHandFollowOffset * Screen.width + (handPos.x * 0.3f);
            xInertia = Mathf.MoveTowards(xInertia, -(gunBase.transform.localPosition.x) + offset, Time.deltaTime * 240f);
            if (100f < Mathf.Abs(gunBase.transform.localPosition.x))
            {
                xInertia = 0f;
                gunBase.transform.localPosition = new Vector3(Mathf.Clamp(gunBase.transform.localPosition.x, -99f, +99f), 0f);
            }
            /*if (holdsSlide && Mathf.Abs(gunBase.transform.localPosition.x) < Mathf.Abs(xInertia))
             *            {
             *                gunBase.transform.localPosition = Vector3.zero;
             *                xInertia = 0f;
        }*/

            // DOnt touch that fix and dont try to use MoveTowardsAngle
            if (300f < gunBase.transform.eulerAngles.z)
                gunBase.transform.rotation = Quaternion.identity;
            rotInertia = Mathf.Clamp(Mathf.MoveTowards(rotInertia, -gunBase.transform.eulerAngles.z, Time.deltaTime * 240f), -16f, +16f);
        }

        private void HandleMag()
        {
            if (0f < magSyncIgnoreTime)
            {
                magSyncIgnoreTime -= Time.deltaTime;
                if (null == insertedMagazine)
                    return;
                UnityEngine.Debug.Log("lol");
                insertedMagazine.transform.localPosition += new Vector3(info.magazineXSpeed * Time.deltaTime, info.magazineYSpeed * Time.deltaTime);
                insertedMagazine.color = new Color(1f, 1f, 1f, insertedMagazine.color.a - (Time.deltaTime / info.magazineAnimationTime)); // evil division
                return;
            }
            string currentMag = gun.CurrentMag();
            if (currentMag == insertedMagazineType)
                return;
            insertedMagazineType = currentMag;
            if (null != insertedMagazine)
                UnityEngine.Object.Destroy(insertedMagazine);
            if (string.IsNullOrEmpty(currentMag))
                return;
            insertedMagazine = new GameObject("InsertedMagazine").AddComponent<Image>();
            insertedMagazine.transform.SetParent(gunBase.transform);
            insertedMagazine.transform.localScale = new Vector3((1f/2560f)*Screen.width, (1f/1440f)*Screen.height);
            insertedMagazine.transform.localPosition = new Vector3(info.magazineXPosition, info.magazineYPosition);
            insertedMagazine.sprite = info.magazineSprites[currentMag];
            insertedMagazine.transform.SetAsFirstSibling();
            insertedMagazine.color = new Color(1f, 1f, 1f, windowAlpha);
        }

        private void HandleCasings()
        {
            if (ushort.MaxValue == casingIndex)
                return;

            casingTimer += Time.deltaTime;
            if (casingTimer < info.casingDelay)
                return;
            casingIndex += 1;
            casingTimer = 0f;
            if (casingIndex >= info.casingSprites.Length)
            {
                casingIndex = ushort.MaxValue;
                casingAnim.enabled = false;
            }
            casingAnim.sprite = info.casingSprites[casingIndex];
        }

        void MagRemoveButton()
        {
            magSyncIgnoreTime = info.magazineAnimationTime;
            gun.RemoveMag();
        }

        private void HandleBandolier()
        {
            Item tmp = body.GetWearableBySlotID("bandolier");
            if (null == tmp || !tmp.TryGetComponent<Container>(out bandolier))
            {
                if (ammoSelector.gameObject.activeSelf)
                {
                    ammoSelector.gameObject.SetActive(false);
                    bandolierBase.gameObject.SetActive(false);
                    ammoSelectWindow.gameObject.SetActive(false);
                    holding = null;
                    if (null != holdingInMinigame)
                        UnityEngine.Object.Destroy(holdingInMinigame);
                }
                shouldntRefreshBandolierCount = false;
                return;
            }
            ammoSelector.gameObject.SetActive(true);
            bandolierBase.gameObject.SetActive(true);
            if (!shouldntRefreshBandolierCount)
            {
                ammoInBand = CountAllSpecificIdInContainerWithOffsetOfOne(bandolier.transform, info.ammos[info.dCurrentAmmo]);
                shouldntRefreshBandolierCount = true;
                for (int i = 1; i < 23; i ++)
                {
                    if (i >= ammoInBand)
                        ptrs[i].sprite = ptrBlank;
               else     ptrs[i].sprite = info.ammosInPtr[info.dCurrentAmmo];
                }
            }
        }

        void HandleFannyPack()
        {
            bool activateWindows = info.canUseFannyPack && PlayerCamera.main.body.HasWearable("fannypack");
            fannyPack.gameObject.SetActive(activateWindows);

            shouldUpdateMagazineCount = shouldUpdateMagazineCount || 0f >= windowAlpha;
            if (shouldUpdateMagazineCount&&activateWindows && 0f < windowAlpha)
            {
                UnityEngine.Debug.Log("lol"); // :tourniqet:
                shouldUpdateMagazineCount = false;
                Dictionary<string, int> itemsInFannyPack = CountAllItemsInContainer(PlayerCamera.main.body.GetWearableBySlotID("torsofront").transform);
                int free = info.maximumMagazines;

                foreach (Image go in placedMagazines)
                    if (null != go)
                        UnityEngine.Object.Destroy(go.gameObject);
                placedMagazines.Clear();

                int placed = 0;
                foreach (KeyValuePair<string, int> kvp in itemsInFannyPack)
                {
                    if (!info.magazineSprites.TryGetValue(kvp.Key, out Sprite sprite))
                        return;

                    int place;
                    if (kvp.Value > free)
                    {
                        place = free;
                        free = 0;
                    }
               else {
                        place = kvp.Value;
                        free -= place;
                    }
                    for (int i = 0; i < place; i++)
                    {
                        Image transistor = new GameObject("FannyPackMagazine").AddComponent<Image>();
                        transistor.transform.SetParent(fannyPack.transform);
                        transistor.transform.localScale = new Vector3((1f/2560f)*Screen.width, (1f/1440f)*Screen.height);
                        transistor.transform.localPosition = new Vector3(((80f/2560f) + (placed * info.magazinesOffset)) *Screen.width, (-60f/1440f)*Screen.height);
                        transistor.sprite = sprite;
                        placedMagazines.Add(transistor);
                        placed += 1;
                    }

                    if (0 >= free)
                        break;
                }

                fannyPackImage.transform.SetAsLastSibling();
                fannyPackZip.transform.SetAsLastSibling();
            }

            if (fannyPack.gameObject.activeSelf)
            {
                fannyPackZipInertia = Mathf.MoveTowardsAngle(fannyPackZipInertia, -fannyPackZip.transform.eulerAngles.z, Time.deltaTime * 25f);
                fannyPackZip.transform.Rotate(0, 0, fannyPackZipInertia);
                if (160f < fannyPackZip.transform.eulerAngles.z && 200f > fannyPackZip.transform.eulerAngles.z)//160-200
                    fannyPackZipInertia = 0f;
            }
        }

        public void SpinBandolied()
        {
            if (spinningBandol)
                return;
            StartCoroutine(_SpinBandolied());
            spinningBandol= true;
        }

        private IEnumerator _SpinBandolied()
        {
            ammoInBand -= 1; // TODO Dead field?
            float timer = 0f;
            ptrBase.transform.localPosition = Vector3.zero;
            while (0.2f > timer)
            {
                ptrBase.transform.localPosition += new Vector3((-150f/2560f)*Screen.width * Time.deltaTime, 0f, 0f);
                timer += Time.deltaTime;
                yield return null;
            }
            /*            if (0 < ammoInBand)
             *            {
             *                ptrs[1].sprite = info.ammosInPtr[info.dCurrentAmmo];
             *                ptrs[ammoInBand].sprite = ptrBlank;
        }*/
            shouldntRefreshBandolierCount = false;
            ptrBase.transform.localPosition = Vector3.zero;
            spinningBandol = false;
            Update();
        }

        public void CreateCasing()
        {
            if (null == info || null == info.casingSprites || 0 == info.casingSprites.Length)
                return;
            casingAnim.enabled = true;
            casingIndex = 0;
            casingAnim.sprite = info.casingSprites[casingIndex];
        }

        public void SliderClickDown()
        {
            holdsSlide = true;
            sliderHoldOffset = sliderFrontImage.transform.localPosition.x;
            onBeginHoldSlide = handPos;
        }

        public void BandolierClickDown()
        {
            if (Input.GetKeyDown(KeyCode.Mouse1))
                return;
            holding = TryGetItemFromContianerById(bandolier.transform, info.ammos[info.dCurrentAmmo]);
            if (null == holding)
                return;
            ptrs[1].sprite = ptrBlank;
            PlayerCamera.main.PlayUISound(PlayerCamera.UISoundType.MiniClick);
            ignoreNextMouseKey = true;
            handImage.sprite = MinigameBase.main.handSprites[3];
            holdingInMinigame = new GameObject("HoldingAmmo");
            holdingInMinigame.transform.SetParent(handTransform);
            holdingInMinigame.transform.localScale = Vector3.one;
            holdingInMinigame.transform.localPosition = new Vector3(-195f, 600f);
            holdingInMinigame.AddComponent<Image>().sprite = info.ammosInHand[info.dCurrentAmmo];
            holdingInMinigame.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 160f);
            receiverTrigger.gameObject.SetActive(true);
            handIsInBandolier = true;
        }

        public void BandolierExitPtr()
        {
            handIsInBandolier = false;
        }

        public void BandolierEnterPtr()
        {
            handIsInBandolier = true;
        }

        public static Item TryGetItemFromContianerById(Transform cont, string target)
        {
            foreach (Transform trans in cont)
            {
                if (!trans.TryGetComponent<Item>(out Item it))
                    continue;
                if (it.id != target)
                    continue;
                return it;
            }
            return null;
        }

        public static int CountAllSpecificIdInContainerWithOffsetOfOne(Transform cont, string target)
        {
            int count = 1;
            foreach (Transform trans in cont)
            {
                if (!trans.TryGetComponent<Item>(out Item it))
                    continue;
                if (it.id != target)
                    continue;
                count += 1;
            }
            return count;
        }

        public static int[] CountAllSpecificIdsInContainerFromListToList(Transform cont, string[] arr)
        {
            if (0 == arr.Length)
                return new int[0] { };
            int[] result = new int[arr.Length];
            foreach (Transform trans in cont)
            {
                if (!trans.TryGetComponent<Item>(out Item it))
                    continue;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] == it.id)
                    {
                        result[i] += 1;
                        break;
                    }
                }
            }
            return result;
        }


        public static Dictionary<string, int> CountAllItemsInContainer(Transform cont)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (Transform transgender in cont)
            {
                if (!transgender.TryGetComponent<Item>(out Item it))
                    continue;
                if (result.ContainsKey(it.id))
                    result[it.id] += 1;
           else     result[it.id]  = 1;
            }
            return result;
        }

        public void ReceiverDrop()
        {
            if (null == holding)
                return;
            if (gun.DragOnto(holding))
                SpinBandolied();
       else { // TODO Test MP
                PlayerCamera.main.PlayUISound(PlayerCamera.UISoundType.Deny);
                holding = null;
            }
        }

        public void HideShow()
        {
            if (!hiding)
                StartCoroutine(_HideShow());
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void HandleWindowAlpha()
        {
            if (0f == windowAlpha || 1f == windowAlpha)
            {
                if (lastWindowAlphaWasLimit)
                    return;
                lastWindowAlphaWasLimit = true;
            }
       else     lastWindowAlphaWasLimit = false;
            Color colo = new Color(1f, 1f, 1f, windowAlpha);
            uiImage.color = colo;
            mainImage.color = colo;
            sliderFrontImage.color = colo;
            sliderBackImage.color = colo;
            rackedSpecific.color = colo;
            hideShowButton.color = colo;
            ammoSelector.color = colo;
            ammoSelectWindow.color = colo;
            ammoIcon.color = colo;
            bandolierBase.color = colo;
            bandolierFront.color = colo;
            ammoSelectCursor.color = colo;
            fannyPackImage.color = colo;
            fannyPackZip.color = colo;
            foreach (Image i in ptrs)
                i.color = colo;
            foreach (Image i in placedMagazines)
                i.color = colo;
            if (null != insertedMagazine)
                insertedMagazine.color = colo;
        }

        private IEnumerator _HideShow()
        {
            hiding = true;
            try
            {
                Vector3 target;
                if (hiden)
                    target = new Vector3(0f, +(1255f/1440f)*Screen.height, 0f);
                else     target = new Vector3(0f, -(1255f/1440f)*Screen.height, 0f);
                float timer = 0f;
                while (0.2f > timer)
                {
                    uiBase.transform.localPosition += target * Time.deltaTime;
                    timer += Time.deltaTime;
                    yield return null;
                }
                if (hiden)
                {
                    uiBase.transform.localPosition = new Vector3(0f, -415f, 0f);
                    hideShowButton.sprite = hideSprite;
                }
                else {
                    uiBase.transform.localPosition = new Vector3(0f, -666f, 0f);
                    hideShowButton.sprite = showSprite;
                }
                hiden = !hiden;
            }
            finally
            {
                shouldntRefreshBandolierCount = false;
                hiding = false;
            }
        }

        public static bool IsAnyGameUIOpen()
        {
            if (ConsoleScript.instance != null && ConsoleScript.instance.active)
            {
                return true;
            }
            PlayerCamera main = PlayerCamera.main;
            if (main != null)
            {
                if (main.woundView != null && main.woundView.activeSelf)
                {
                    return true;
                }
                if (main.craftingPanel != null && main.craftingPanel.activeSelf)
                {
                    return true;
                }
                if (main.tradeMenu != null && main.tradeMenu.activeSelf)
                {
                    return true;
                }
                if (main.radialOpen)
                {
                    return true;
                }
            }
            if (MinigameBase.main != null && MinigameBase.main.currentMinigame != null)
            {
                return true;
            }
            return PauseHandler.main.isPaused;
        }

        public void ToggleAmmo()
        { // // is that enough conversions for you?!
            //info.dCurrentAmmo = (ushort)(ushort)((ushort)((ushort)info.dCurrentAmmo+(ushort)1) % (ushort)info.ammos.Length);
            //ammoIcon.sprite = info.ammosIcons[info.dCurrentAmmo + 2];
            ammoSelectWindow.gameObject.SetActive(!ammoSelectWindow.gameObject.activeSelf);
            EventSystem.current.SetSelectedGameObject(null);
            if (ammoSelectWindow.gameObject.activeSelf)
            {
                PlayerCamera.main.PlayUISound(PlayerCamera.UISoundType.Click);
            }
       else     PlayerCamera.main.PlayUISound(PlayerCamera.UISoundType.Close);
        }

        public void SetAmmoType(ushort index)
        {
            if (index > info.ammos.Length)
                throw new Exception("[GunMinigame] Requested ammo index is out of bountry! :tourniqet:");
            info.dCurrentAmmo = index;
            ammoIcon.sprite = info.ammosIcons[info.dCurrentAmmo];
            StartCoroutine(_SetAmmoType());
            PlayerCamera.main.PlayUISound(PlayerCamera.UISoundType.MiniClick);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private IEnumerator _SetAmmoType()
        {
            Vector3 finish = ammoSelectAmmo[info.dCurrentAmmo].transform.localPosition;
            Vector3 change = (ammoSelectCursor.transform.localPosition - finish) * 0.8f;
            float timer = 0f;
            while (0.2f > timer)
            {
                ammoSelectCursor.transform.localPosition -= change * timer;
                timer += Time.deltaTime;
                yield return null;
            }
            ammoSelectCursor.transform.localPosition = finish;
        }
    }

    public class ImageHoverror : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            MinigameManager.GetOrAddInstance().handIsHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            MinigameManager.GetOrAddInstance().handIsHovered = false;
        }
    }

    public class AlphaRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private Image myImage;

        void Awake()
        {
            myImage = GetComponent<Image>();
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (myImage == null) return true;

            // Convert screen point to local position on the RectTransform
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                myImage.rectTransform, sp, eventCamera, out localPoint);

            // Convert local position to texture coordinate space (0 to 1)
            Rect rect = myImage.rectTransform.rect;
            float x = (localPoint.x - rect.x) / rect.width;
            float y = (localPoint.y - rect.y) / rect.height;

            try
            {
                // Read the pixel alpha directly from the sprite texture
                Texture2D tex = myImage.sprite.texture;

                // Convert normalized coordinates to pixel coordinates
                int pixelX = Mathf.FloorToInt(x * tex.width);
                int pixelY = Mathf.FloorToInt(y * tex.height);

                Color pixelColor = tex.GetPixel(pixelX, pixelY);

                // If alpha is below your threshold, return false so the ray passes through
                return pixelColor.a >= myImage.alphaHitTestMinimumThreshold;
            }
            catch
            {
                // Fallback if the texture is not readable
                return true;
            }
        }
    }
}
