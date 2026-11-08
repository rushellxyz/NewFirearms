using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace GunMinigame
{
    // Read the final gun pose after the minigame has applied its inertia in LateUpdate.
    [DefaultExecutionOrder(100)]
    internal sealed class MinigameCoordinates : MonoBehaviour
    {
        internal static bool Visible;
        private MinigameManager manager;
        private RectTransform panel;
        private Text label;

        internal void Initialize(MinigameManager owner) => manager = owner;

        private void LateUpdate()
        {
            bool show = Visible && manager != null && manager.active && !manager.hiden &&
                manager.info != null && manager.gunBase != null && manager.canvas != null;
            if (panel != null) panel.gameObject.SetActive(show);
            if (!show) return;
            if (panel == null) CreatePanel();
            panel.SetAsLastSibling();

            var canvas = manager.canvas.GetComponent<Canvas>();
            var canvasRect = manager.canvas.transform as RectTransform;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            if (canvasRect == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRect, Input.mousePosition, camera, out Vector3 world))
            {
                panel.gameObject.SetActive(false);
                return;
            }

            Vector3 local = manager.gunBase.transform.InverseTransformPoint(world);
            float x = local.x / Mathf.Max(1f, Screen.width);
            float y = local.y / Mathf.Max(1f, Screen.height);
            float inertiaX = local.x / Mathf.Max(1f, MinigameManager.size.x);
            float inertiaY = local.y / Mathf.Max(1f, MinigameManager.size.y);
            bool rotating = manager.info.rackByRotation && !manager.info.mosynStyleRack;
            Vector2 direction = (Vector2)local - (Vector2)manager.sliderFrontImage.transform.localPosition;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            label.text = string.Format(CultureInfo.InvariantCulture,
                "nf_coordinate\n" +
                "casing / magazine / cover / barrel / rack center\n" +
                "X: {0:0.00000}    Y: {1:0.00000}\n" +
                "inertiaHand / inertiaBarrel\n" +
                "X: {2:0.00000}    Y: {3:0.00000}\n" +
                (rotating ? "slider: {4:0.000} degrees" : "slider X: {0:0.00000}"),
                x, y, inertiaX, inertiaY, angle);
        }

        private void CreatePanel()
        {
            var background = new GameObject("Minigame mouse coordinates", typeof(RectTransform), typeof(Image))
                .GetComponent<Image>();
            panel = background.rectTransform;
            panel.SetParent(manager.canvas.transform, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(12f, -12f);
            background.color = new Color(0f, 0f, 0f, 0.85f);
            background.raycastTarget = false;

            label = new GameObject("Coordinates", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(panel, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10f, 8f);
            label.rectTransform.offsetMax = new Vector2(-10f, -8f);
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = Mathf.Max(12, Mathf.RoundToInt(16f * Screen.height / 1440f));
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.raycastTarget = false;
            label.supportRichText = false;
            panel.sizeDelta = new Vector2(label.fontSize * 30f, label.fontSize * 8f);
        }

        private void OnDisable()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel.gameObject);
        }
    }
}
