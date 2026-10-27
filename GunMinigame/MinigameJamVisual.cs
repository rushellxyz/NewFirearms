using UnityEngine;
using UnityEngine.UI;

namespace GunMinigame
{
    internal sealed class MinigameJamVisual : MonoBehaviour
    {
        private MinigameManager manager;
        private Image image;
        private IMinigameJammedGun jammedGun;
        private float nextClearTime;

        internal void Initialize(MinigameManager owner) => manager = owner;

        private void LateUpdate()
        {
            jammedGun = manager.gun as IMinigameJammedGun;
            bool show = manager.active && manager.info != null && jammedGun != null &&
                jammedGun.TryGetJam(out _, out _);
            if (image != null) image.gameObject.SetActive(show);
            if (!show) return;
            jammedGun.TryGetJam(out var sprite, out bool liveRound);
            if (image == null)
            {
                image = new GameObject("JammedRound").AddComponent<Image>();
                image.transform.SetParent(manager.gunBase.transform, false);
                image.gameObject.AddComponent<Button>().onClick.AddListener(Clear);
                image.preserveAspect = true;
            }
            image.transform.SetAsLastSibling();
            image.sprite = Casing.GetVisualSprite(sprite);
            image.color = new Color(1f, 1f, 1f, manager.windowAlpha);
            //image.rectTransform.sizeDelta = new Vector2(108f * Screen.width / 2560f, 192f * Screen.height / 1440f);
            // чет тупит тут
            image.transform.localScale = new Vector3((9f/2560f)*Screen.width, (9f/1440f)*Screen.height);
            image.rectTransform.sizeDelta =  liveRound ? new Vector2(12, 26) : new Vector2(9, 19);

            image.transform.localPosition = new Vector3(manager.info.casingXPosition, manager.info.casingYPosition);
            image.transform.localRotation = Quaternion.Euler(0f, 0f, liveRound ? -35f : 25f);
        }

        private void Clear()
        {
            if (Time.unscaledTime < nextClearTime || !manager.active || manager.hiden ||
                MinigameManager.IsAnyGameUIOpen()) return;
            nextClearTime = Time.unscaledTime + 0.3f;
            jammedGun?.ClearJam();
        }

    }
}
