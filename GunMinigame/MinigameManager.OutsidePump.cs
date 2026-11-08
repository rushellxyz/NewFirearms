using UnityEngine;
using UnityEngine.UI;

namespace GunMinigame
{
    public partial class MinigameManager
    {
        private readonly PumpGesture outsidePump = new PumpGesture();
        private bool outsidePumpRequired, outsidePumpInitialized, outsidePumpOpened;
        private bool? outsidePumpRequest;
        private float outsidePumpRequestTime;
        private RectTransform pumpGuideRoot;
        private Image pumpGuideLine, pumpGuideStart, pumpGuideEnd, pumpGuideCursor;

        public bool hadRacked;

        private void BeginOutsidePumpAfterShot()
        {
            if (!info.pumpOutsideWindow) return;
            outsidePumpRequired = true;
            outsidePumpInitialized = outsidePumpOpened = false;
            outsidePumpRequest = null;
            hadRacked = false;
        }

        private void ResetOutsidePump()
        {
            outsidePumpRequired = outsidePumpInitialized = outsidePumpOpened = false;
            outsidePumpRequest = null;
            hadRacked = false;
            HidePumpGuide();
        }

        private void HidePumpGuide()
        {
            if (pumpGuideRoot != null) pumpGuideRoot.gameObject.SetActive(false);
        }

        private bool HandleOutsidePump()
        {
            if (!info.pumpOutsideWindow || !outsidePumpRequired || !supportGripHeld) return false;
            if (gun is IMinigameJammedGun jammed && jammed.TryGetJam(out _, out _))
            {
                ResetOutsidePump();
                return false;
            }
            if (!outsidePumpInitialized)
            {
                // A cursor at screen center has no usable travel axis; wait until it moves away.
                outsidePumpInitialized = outsidePump.Begin(Input.mousePosition,
                    new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), Mathf.Max(32f, Screen.height * 0.05f));
                if (!outsidePumpInitialized) return false;
            }

            bool racked = gun.IsRacked();
            if (outsidePumpRequest.HasValue)
            {
                if (racked == outsidePumpRequest.Value) outsidePumpRequest = null;
                else if (Time.unscaledTime - outsidePumpRequestTime >= 1.5f)
                    outsidePumpRequest = null;
            }
            if (racked) outsidePumpOpened = true;

            float progress = outsidePump.Progress(Input.mousePosition);
            float position = Mathf.Lerp(info.unrackedIdlePosition, info.rackedIdlePosition, progress);
            position = SliderRange.Clamp(position, info.sliderMinimumPosition, info.sliderMaximumPosition);
            if (!outsidePumpRequest.HasValue && SliderRange.ShouldToggle(position, racked,
                info.sliderMinimumPosition, info.sliderMaximumPosition, info.unrackedToRacked, info.rackedToUnracked))
            {
                outsidePumpRequest = !racked;
                outsidePumpRequestTime = Time.unscaledTime;
                gun.Rack();
                if (hadRacked)
                    ResetOutsidePump();
                hadRacked = true;
            }
            sliderFrontImage.transform.localPosition = new Vector3(position, 0f);
            sliderBackImage.transform.localPosition = new Vector3(position, 0f);
            AddSliderImpulse(new Vector2(position - sliderLastXPos, 0f));
            sliderLastXPos = position;
            DrawPumpGuide(progress);
            if (outsidePumpOpened && !gun.IsRacked() && !outsidePumpRequest.HasValue && progress <= 0.02f)
                ResetOutsidePump();
            return true;
        }

        private Image CreatePumpGuideImage(string name, Color color)
        {
            var image = new GameObject(name).AddComponent<Image>();
            image.transform.SetParent(pumpGuideRoot, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Vector2 PumpGuidePoint(Vector2 screenPosition)
        {
            Canvas parentCanvas = canvas.GetComponent<Canvas>();
            Camera camera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(pumpGuideRoot, screenPosition, camera, out Vector2 point);
            return point;
        }

        private void DrawPumpGuide(float progress)
        {
            if (!MinigameVisualization.Visible)
            {
                HidePumpGuide();
                return;
            }
            if (pumpGuideRoot == null)
            {
                pumpGuideRoot = new GameObject("OutsidePumpGuide", typeof(RectTransform)).GetComponent<RectTransform>();
                pumpGuideRoot.SetParent(uiBase.transform, false);
                pumpGuideLine = CreatePumpGuideImage("Travel", new Color(0.4f, 0.8f, 1f, 0.45f));
                pumpGuideStart = CreatePumpGuideImage("PositionA", Color.white);
                pumpGuideEnd = CreatePumpGuideImage("PositionB", Color.white);
                pumpGuideCursor = CreatePumpGuideImage("ForendPosition", Color.cyan);
            }
            pumpGuideRoot.gameObject.SetActive(true);
            Vector2 start = PumpGuidePoint(outsidePump.Start);
            Vector2 end = PumpGuidePoint(outsidePump.End);
            Vector2 axis = end - start;
            pumpGuideLine.rectTransform.localPosition = (start + end) * 0.5f;
            pumpGuideLine.rectTransform.sizeDelta = new Vector2(axis.magnitude, 2f);
            pumpGuideLine.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
            pumpGuideStart.rectTransform.localPosition = start;
            pumpGuideEnd.rectTransform.localPosition = end;
            pumpGuideStart.rectTransform.sizeDelta = pumpGuideEnd.rectTransform.sizeDelta = Vector2.one * 6f;
            pumpGuideCursor.rectTransform.localPosition = Vector2.Lerp(start, end, progress);
            pumpGuideCursor.rectTransform.sizeDelta = Vector2.one * 9f;
        }
    }
}
