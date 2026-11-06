using UnityEngine;

namespace GunMinigame
{
    internal sealed class PumpGesture
    {
        internal Vector2 Start { get; private set; }
        internal Vector2 End { get; private set; }

        internal bool Begin(Vector2 start, Vector2 end, float minimumDistance)
        {
            // коостыльный фикс бага от codex который не может его исправить изза длины чата
            if (MinigameManager.instance.gun.IsReady())
                return false;

            if ((end - start).sqrMagnitude < minimumDistance * minimumDistance) return false;
            Start = start;
            End = end;
            return true;
        }

        internal float Progress(Vector2 pointer)
        {
            Vector2 axis = End - Start;
            return axis.sqrMagnitude > 0f
                ? Mathf.Clamp01(Vector2.Dot(pointer - Start, axis) / axis.sqrMagnitude) : 0f;
        }
    }
}
