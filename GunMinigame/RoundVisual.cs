using UnityEngine;

namespace GunMinigame
{
    internal static class RoundVisual
    {
        internal static Vector2 GetSize(Sprite sprite, float targetHeight, bool liveRound)
        {
            // Zero keeps the original bounding boxes, including their aspect-ratio fitting.
            if (sprite == null || targetHeight <= 0f || float.IsNaN(targetHeight) ||
                float.IsInfinity(targetHeight) || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
                return liveRound ? new Vector2(12f, 26f) : new Vector2(9f, 19f);

            return new Vector2(targetHeight * (sprite.rect.width / sprite.rect.height), targetHeight);
        }
    }
}
