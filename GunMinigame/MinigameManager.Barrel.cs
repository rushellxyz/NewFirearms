using UnityEngine;
using UnityEngine.UI;

namespace GunMinigame
{
    public partial class MinigameManager
    {
        private Image barrelImage;

        private void UpdateBarrelTransform()
        {
            if (!barrelImage.enabled) return;
            barrelImage.transform.localPosition = new Vector3(info.barrelXPosition, info.barrelYPosition, 0f);
            float progress = Mathf.InverseLerp(info.sliderMaximumPosition, info.sliderMinimumPosition,
                sliderFrontImage.transform.localPosition.x);
            float angle = Mathf.Lerp(0f, 10f, progress);
            barrelImage.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            barrelImage.color = new Color(1f, 1f, 1f, windowAlpha);
        }
    }
}
