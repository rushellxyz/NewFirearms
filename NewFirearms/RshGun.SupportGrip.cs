using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun : GunMinigame.IMinigameSupportGrip
    {
        private Body supportGripBody;
        private float supportGripUntil;

        public void SetSupportGrip(bool held)
        {
            ApplySupportGrip(PlayerCamera.main.body, held);
        }

        internal void ApplySupportGrip(Body holder, bool held)
        {
            if (!held)
            {
                if (supportGripBody == holder) supportGripBody = null;
                return;
            }
            if (!CanSupportGrip(holder)) return;
            supportGripBody = holder;
            // A disconnected client or interrupted UI cannot leave a permanent recoil bonus.
            supportGripUntil = Time.unscaledTime + 0.75f;
        }

        private bool CanSupportGrip(Body holder)
            => holder != null && holder.conscious && (holder.handSlot == 0 || holder.handSlot == 1) &&
                holder.GetItem(holder.handSlot) == it && holder.GetItem(1 - holder.handSlot) == null &&
                prop?.minigame != null && prop.minigame.HasSupportGrip;

        private bool IsSupportGripActive(Body holder)
            => supportGripBody == holder && Time.unscaledTime < supportGripUntil && CanSupportGrip(holder);

        private float GetMuzzleRise(Body holder, float muzzleRise)
            => IsSupportGripActive(holder) ? muzzleRise * 0.5f : muzzleRise;

        private void UpdateSupportGripState()
        {
            if (supportGripBody != null &&
                (Time.unscaledTime >= supportGripUntil || !CanSupportGrip(supportGripBody)))
                supportGripBody = null;
        }
    }
}
