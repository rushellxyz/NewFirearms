using UnityEngine;

// Не пойму почему он вдруг все вынес в отдельный класс, но ладно. 

namespace GunMinigame
{
    internal static class SliderRange
    {
        // Config endpoints describe racking direction, independently of numeric order.
        internal static float Clamp(float position, float minimum, float maximum)
            => Mathf.Clamp(position, Mathf.Min(minimum, maximum), Mathf.Max(minimum, maximum));

        internal static bool ShouldToggle(float position, bool racked, float minimum, float maximum, float rackPoint)
        {
            float offset = (position - rackPoint) * (minimum <= maximum ? 1f : -1f);
            return racked ? offset > 0f : offset < 0f;
        }

        internal static bool AtMaximum(float position, float minimum, float maximum)
            => minimum <= maximum ? position >= maximum : position <= maximum;
    }
}
