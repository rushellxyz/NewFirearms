using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GunMinigame
{
    public enum GrabType { Flat, Curved, Vertical }

    public partial class MinigameManager
    {
        internal Image grabTriggerImage;
        private Image supportHandImage;
        private GameObject supportGripInputBlocker;
        private bool supportGripHeld;
        private Vector3 supportGripPoint;
        private float nextSupportGripUpdate;

        private void PrepareSupportGrip()
        {
            grabTriggerImage = new GameObject("MinigameGrabTrigger").AddComponent<Image>();
            grabTriggerImage.transform.SetParent(gunBase.transform, false);
            grabTriggerImage.rectTransform.sizeDelta = size;
            grabTriggerImage.color = new Color(1f, 1f, 1f, 0f);
            grabTriggerImage.alphaHitTestMinimumThreshold = 0.1f;
            grabTriggerImage.gameObject.AddComponent<AlphaRaycastFilter>();
            grabTriggerImage.gameObject.AddComponent<SupportGripTrigger>().Initialize(this);
            grabTriggerImage.gameObject.SetActive(false);

            supportHandImage = new GameObject("MinigameSupportHand").AddComponent<Image>();
            supportHandImage.transform.SetParent(gunBase.transform, false);
            supportHandImage.rectTransform.pivot = Vector2.one;
            supportHandImage.transform.localScale = new Vector3(Screen.width * (1.25f/2560f), Screen.height * (1.25f/1440f), 1f);
            supportHandImage.raycastTarget = false;
            supportHandImage.preserveAspect = true;
            supportHandImage.gameObject.SetActive(false);

            // Catch left clicks over the entire window before any gun, magazine, or button handler.
            var blocker = new GameObject("SupportGripInputBlocker").AddComponent<Image>();
            blocker.transform.SetParent(maskBase.transform, false);
            blocker.rectTransform.sizeDelta = size;
            blocker.color = Color.clear;
            blocker.gameObject.AddComponent<SupportGripInputFilter>();
            blocker.gameObject.AddComponent<EventTrigger>();
            supportGripInputBlocker = blocker.gameObject;
            supportGripInputBlocker.SetActive(false);
        }

        private bool CanSupportGrip()
        {
            var component = gun as Component;
            var item = component != null ? component.GetComponent<Item>() : null;
            return active && !hiden && !hiding && !automatedHandAction && !holdsSlide && holding == null &&
                !IsAnyGameUIOpen() && body != null && body.conscious &&
                (body.handSlot == 0 || body.handSlot == 1) && item != null &&
                body.GetItem(body.handSlot) == item && body.GetItem(1 - body.handSlot) == null &&
                info.HasSupportGrip && gun is IMinigameSupportGrip;
        }

        internal void BeginSupportGrip(Vector2 pointer)
        {
            if (supportGripHeld || Input.GetMouseButton(0) || !CanSupportGrip()) return;
            Transform anchor = info.grabOnSlider ? sliderFrontImage.transform : gunBase.transform;
            supportHandImage.transform.SetParent(anchor, false);
            supportGripPoint = info.grabOnSlider
                ? new Vector3(info.inertiaBarrelXPosition * size.x, info.inertiaBarrelYPosition * size.y)
                : anchor.InverseTransformPoint(pointer);
            supportGripHeld = true;
            nextSupportGripUpdate = 0f;
            UpdateSupportGrip();
        }

        private void EndSupportGrip()
        {
            HidePumpGuide();
            if (supportGripHeld) (gun as IMinigameSupportGrip)?.SetSupportGrip(false);
            supportGripHeld = false;
            if (handImage != null) handImage.enabled = true;
            if (supportHandImage != null) supportHandImage.gameObject.SetActive(false);
            if (supportGripInputBlocker != null) supportGripInputBlocker.SetActive(false);
        }

        private void UpdateSupportGrip()
        {
            if (!supportGripHeld) return;
            if (!Input.GetMouseButton(1) || !CanSupportGrip())
            {
                EndSupportGrip();
                return;
            }
            supportHandImage.sprite = info.grabHandSprite;
            supportHandImage.rectTransform.sizeDelta = info.grabHandSprite.rect.size;
            supportHandImage.transform.localPosition = supportGripPoint;
            supportHandImage.color = new Color(1f, 1f - body.averagePain * 0.01f,
                1f - body.averagePain * 0.01f, windowAlpha);
            supportHandImage.gameObject.SetActive(true);
            supportGripInputBlocker.SetActive(true);
            handImage.enabled = false;
            if (Time.unscaledTime >= nextSupportGripUpdate)
            {
                nextSupportGripUpdate = Time.unscaledTime + 0.25f;
                ((IMinigameSupportGrip)gun).SetSupportGrip(true);
            }
        }
    }

    internal sealed class SupportGripInputFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 point, Camera camera)
            => Input.GetMouseButton(1) || Input.GetMouseButtonUp(1);
        // codex, фиксю баги за тобой
        // интересно, но это в первый раз когда кодес прям оставил опечатку (тут 0 было и захват не работал тк это другая кнопка мыши)
    }

    internal sealed class SupportGripTrigger : MonoBehaviour, IPointerDownHandler
    {
        private MinigameManager manager;
        internal void Initialize(MinigameManager owner) => manager = owner;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                manager.BeginSupportGrip(eventData.position);
        }
    }
}
