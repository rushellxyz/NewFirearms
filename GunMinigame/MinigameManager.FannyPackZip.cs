using UnityEngine;

namespace GunMinigame
{
    public partial class MinigameManager
    {
        private float zipStepDirection = 1f;

        private void AddFannyPackZipImpulse(float impulse)
        {
            if (fannyPackZip == null || !fannyPackZip.gameObject.activeInHierarchy || IsCutsceneOrEnding()) return;
            fannyPackZipInertia = Mathf.Clamp(fannyPackZipInertia + impulse, -360f, 360f);
        }

        private void ResetFannyPackZip()
        {
            fannyPackZipInertia = 0f;
            zipStepDirection = 1f;
            if (fannyPackZip != null) fannyPackZip.transform.localRotation = Quaternion.identity;
        }

        private void UpdateFannyPackZip()
        {
            if (!fannyPackZip.gameObject.activeInHierarchy)
            {
                ResetFannyPackZip();
                return;
            }
            if (PauseHandler.main != null && PauseHandler.main.isPaused) return;
            float remaining = Mathf.Min(Time.deltaTime, 0.1f);
            float angle = Mathf.DeltaAngle(0f, fannyPackZip.transform.localEulerAngles.z);
            while (remaining > 0f)
            {
                float dt = Mathf.Min(remaining, 1f / 240f);
                // A damped pendulum; inertia is angular velocity in degrees per second.
                fannyPackZipInertia += (-angle * 50f - fannyPackZipInertia * 6f) * dt;
                angle = Mathf.Clamp(angle + fannyPackZipInertia * dt, -45f, 45f);
                remaining -= dt;
            }
            fannyPackZip.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
