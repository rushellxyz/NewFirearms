// u/ObligationAmazing799
// ^ What is this? I forgot why i left it here
// maybe important, tho, just, uhh, i forgor
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;

using System.Linq;

namespace NewFirearms
{
    // override to render gun menu
    [HarmonyPatch(typeof(PlayerCamera), "HandleGunMenu")]
    public class PlayerCameraPatch1
    {
        public static List<Image> bulletImages = null;

        static bool Prefix(PlayerCamera __instance)
        {
            if (!GunMinigame.Plugin.useMinigame)
                HandleLegacyGunUi(__instance);
       else {
                if (!(__instance.body.HoldingItem(__instance.body.handSlot)))
                {
                    GunMinigame.MinigameManager.GetOrAddInstance().Hide();
                    return false;
                }
                Item it = __instance.body.GetItem(__instance.body.handSlot);
                if (!it.Stats.HasTag("gun"))
                {
                    GunMinigame.MinigameManager.GetOrAddInstance().Hide();
                    return false;
                }

                RshGun rshGun = it.GetComponent<RshGun>();
                if (null != rshGun.prop.minigame)
                {
                    GunMinigame.MinigameManager.GetOrAddInstance().Show(rshGun.prop.minigame, rshGun);

                    // TODO TODO TODO
                    rshGun.fireMode = rshGun.prop.fireModes.Max();
                }
           else     HandleLegacyGunUi(__instance, holdinChecksCanBeIgonredForPerfomnrenr:true);
            }
            return false;
        }

        public static void HandleLegacyGunUi(PlayerCamera __instance, bool holdinChecksCanBeIgonredForPerfomnrenr=false)
        {
            if (null == bulletImages || null == bulletImages[0])
            {
                bulletImages = new List<Image>() { __instance.gunBulletImage };
            }
            if (holdinChecksCanBeIgonredForPerfomnrenr || (__instance.body.HoldingItem(__instance.body.handSlot) && __instance.body.GetItem(__instance.body.handSlot).Stats.HasTag("gun")))
            {
                RshGun rshGun = __instance.body.GetItem(__instance.body.handSlot).GetComponent<RshGun>();
                __instance.gunMenu.SetActive(!__instance.radialOpen);
                if (rshGun.racked)
                    __instance.gunRackImage.sprite = rshGun.prop.unrackSprite;
       else         __instance.gunRackImage.sprite = rshGun.prop.rackSprite;
                __instance.gunMagButton.interactable = !string.IsNullOrEmpty(rshGun.mag) || (3 == rshGun.prop.feedType && rshGun.racked);
                __instance.gunMagButton.transform.GetChild(0).GetComponent<Image>().sprite = rshGun.prop.removeMagSprite;
                __instance.gunSafeImage.sprite = rshGun.prop.fireModesSprite[rshGun.fireMode];
                int chambers = 1;
                if (3 == rshGun.prop.feedType)
                    chambers = rshGun.rounds.Count;
                for (int i = 0; i < rshGun.prop.internalCapacity; i++)
                {
                    if (i >= chambers)
                    {
                        if (i < bulletImages.Count)
                            bulletImages[i].gameObject.SetActive(false);
                        continue;
                    }

                    int bullet = rshGun.rounds[i] + 2;
                    if (rshGun.prop.bulletsSprites.Length < bullet)
                        bullet = rshGun.prop.bulletsSprites.Length - 1;
                    if (bulletImages.Count <= i)
                    {
                        InstantiateNewBulletImage();
                    }
                    bulletImages[i].sprite = rshGun.prop.bulletsSprites[bullet];
                }
                __instance.gunCrosshair.gameObject.SetActive(rshGun.IsReady());
                float num = Vector2.Distance(rshGun.transform.position, __instance.body.targetLookPos);
                if (!__instance.body.isRight)
                {
                    num *= -1f;
                }
                __instance.gunCrosshair.position = Camera.main.WorldToScreenPoint(rshGun.transform.position + rshGun.transform.right * num);
            }
       else {
                __instance.gunMenu.SetActive(value: false);
                __instance.gunCrosshair.gameObject.SetActive(value: false);
            }
        }

        static void InstantiateNewBulletImage()
        {
            GameObject original = bulletImages[bulletImages.Count - 1].gameObject;
            GameObject go = UnityEngine.Object.Instantiate(original);
            Vector2 originalPostion = original.GetComponent<RectTransform>().position;
            go.transform.SetParent(original.transform.parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.position = new Vector2(originalPostion.x + (40f/2560f)*Screen.width, originalPostion.y);
            rect.sizeDelta *= 1.32f;
            bulletImages.Add(go.GetComponent<Image>());
        }
    }

    // override to handle gun rack
    [HarmonyPatch(typeof(PlayerCamera), "GunRack")]
    public class PlayerCameraPatch2
    {
        public static bool Prefix(PlayerCamera __instance)
        {
            if (__instance.body.HoldingItem(__instance.body.handSlot) && __instance.body.GetItem(__instance.body.handSlot).TryGetComponent<RshGun>(out var component))
            {
                component.Rack(manual: true);
            }
            return false;
        }
    }

    // override to handle mag eject
    [HarmonyPatch(typeof(PlayerCamera), "GunEjectMag")]
    public class PlayerCameraPatch3
    {
        public static bool Prefix(PlayerCamera __instance)
        {
            if (__instance.body.HoldingItem(__instance.body.handSlot) && __instance.body.GetItem(__instance.body.handSlot).TryGetComponent<RshGun>(out var component))
            {
                component.RemoveMag(null);
            }
            return false;
        }
    }

    // override to handle fire toggle
    [HarmonyPatch(typeof(PlayerCamera), "GunToggleSafety")]
    public class PlayerCameraPatch4
    {
        public static bool Prefix(PlayerCamera __instance)
        {
            if (__instance.body.HoldingItem(__instance.body.handSlot) && __instance.body.GetItem(__instance.body.handSlot).TryGetComponent<RshGun>(out var component))
            { // TODO i think it eould be great to move it into funciton in RshGun
                if (Plugin.togetherMpEnabled && component.RequestHostIfClient(4))
                    return false;
                Sound.Play(component.prop.toggleFireModeAudio, component.transform.position);
                component.fireMode = component.prop.fireModes[(component.prop.fireModes.IndexOf(component.fireMode) + 1) % component.prop.fireModes.Count];
                if (Plugin.togetherMpEnabled)
                    component.SyncIfHost();
            }
            return false;
        }
    }

    // patch to add rounds amount description
    [HarmonyPatch(typeof(PlayerCamera), "ItemHoverDescription")]
    class PlayerCameraPatch5
    {
        static void Postfix(ref (string, string) __result, Item item)
        {
            if (!Input.GetKey(KeyBinds.GetBind("expanddesc")) || !item.TryGetComponent<RshMag>(out var rshMag))
                return;

            if (!item.Stats.rec.recognizable)
                return;

            __result.Item2 += "<color=#ff8fb0><sprite index=8 tint=1>" + rshMag.TotalRounds() + "/" + rshMag.rounds.Count + " " + Locale.GetOther("magazinerounds") + "\n";
        }
    }
}
