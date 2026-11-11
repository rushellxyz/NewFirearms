using UnityEngine;

namespace GunMinigame
{
    internal static class SliderAnimation
    {
        // Reach about 95% of an idle-pose transition in 0.05 seconds, independently of frame rate.
        internal static float Move(float current, float target, float deltaTime)
        {
            if (deltaTime <= 0f) return current;
            float value = Mathf.Lerp(current, target, 1f - Mathf.Exp(-60f * deltaTime));
            return Mathf.Abs(value - target) <= 0.001f ? target : value;
        }

        internal static float MoveAngle(float current, float target, float deltaTime)
            => Move(current, current + Mathf.DeltaAngle(current, target), deltaTime);

        internal static float Ease(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return progress * progress * (3f - 2f * progress);
        }
    }
}
