using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GunMinigame
{
    internal class MinigameVisualization : MonoBehaviour
    {
        internal static bool Visible;
        private MinigameManager manager;
        private RectTransform overlay;
        private readonly List<Image> dots = new List<Image>();
        private Texture2D texture;
        private Sprite sprite;
        private int used;

        internal void Initialize(MinigameManager owner) => manager = owner;

        private void LateUpdate()
        {
            bool show = Visible && manager != null && manager.active && manager.info != null;
            if (overlay != null) overlay.gameObject.SetActive(show);
            if (!show) return;
            if (overlay == null) CreateOverlay();
            overlay.SetAsLastSibling();
            used = 0;
            MinigameInfo info = manager.info;
            // All configured spawn points and gun parts use the moving gun base.
            if (info.casingsSprite != null)
                Dot("casingXPosition / casingYPosition", new Vector2(info.casingXPosition, info.casingYPosition));
            if (info.magazineSprites != null)
                Dot("magazineXPosition / magazineYPosition", new Vector2(info.magazineXPosition, info.magazineYPosition));
            if (info.coverAnimation != null && info.coverAnimation.Length > 0)
                Dot("coverXPosition / coverYPosition", new Vector2(info.coverXPosition, info.coverYPosition));
            if (info.rackByRotation)
                Dot("rackRotationCenterXPosition / rackRotationCenterYPosition", new Vector2(info.rackRotationCenterXPosition, info.rackRotationCenterYPosition));
            /*else if (info.sliderFrontSprite != null || info.sliderBackSprite != null)
            {
                Dot("sliderMinimumPosition", new Vector2(info.sliderMinimumPosition, 0f));
                Dot("sliderMaximumPosition", new Vector2(info.sliderMaximumPosition, 0f));
                Dot("rackPoint", new Vector2(info.rackPoint, 0f));
                Dot("rackedIdlePosition", new Vector2(info.rackedIdlePosition, 0f));
                Dot("unrackedIdlePosition", new Vector2(info.unrackedIdlePosition, 0f));
            }*/
            for (int i = used; i < dots.Count; i++) dots[i].gameObject.SetActive(false);
        }

        private void CreateOverlay()
        {
            overlay = new GameObject("JSON coordinate markers", typeof(RectTransform)).GetComponent<RectTransform>();
            overlay.SetParent(transform, false);
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    texture.SetPixel(x, y, new Vector2(x - 7.5f, y - 7.5f).sqrMagnitude <= 49f ? Color.white : Color.clear);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f));
        }

        private void Dot(string name, Vector2 position, bool gunSpace = true)
        {
            if (used == dots.Count)
            {
                Image dot = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                dot.transform.SetParent(overlay, false);
                dot.sprite = sprite;
                dot.color = Color.blue;
                dot.raycastTarget = false;
                dots.Add(dot);
            }
            Image image = dots[used++];
            image.name = name;
            image.gameObject.SetActive(true);
            image.rectTransform.sizeDelta = Vector2.one * Mathf.Max(5f, Screen.height * 8f / 1440f);
            image.transform.localPosition = gunSpace
                ? overlay.InverseTransformPoint(manager.gunBase.transform.TransformPoint(position))
                : (Vector3)position;
        }

        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }

    [HarmonyPatch(typeof(ConsoleScript), "RegisterAllCommands")]
    internal static class VisualizationCommandPatch
    {
        private static void Postfix()
        {
            ConsoleScript.Commands.Add(new Command("nf_visualize", Locale.GetOther("gunminigame.command.visualize.description"), args =>
            {
                MinigameVisualization.Visible = !MinigameVisualization.Visible;
                Debug.Log(Locale.GetOther(MinigameVisualization.Visible
                    ? "gunminigame.command.visualize.enabled" : "gunminigame.command.visualize.disabled"));
            }, new Dictionary<int, List<string>>()));
        }
    }
}
