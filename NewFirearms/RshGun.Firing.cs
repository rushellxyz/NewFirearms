using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        public const byte BurstFireMode = 4;
        private int burstShotsRemaining;

        public bool Shoot(Body body = null)
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(0))
            {
                currentlyShooting = true;
                return true;
            }
            // Repeated requests cannot restart or extend a burst already in progress.
            if (burstShotsRemaining > 0 || gasTime > 0f) return false;
            if (!FireRound(body))
            {
                StopFiring();
                return false;
            }
            if (fireMode == BurstFireMode && !IsJammed)
                burstShotsRemaining = 2; // The trigger pull already fired the first round.
            return true;
        }

        private void ContinueFiring()
        {
            if (shooter == null || IsJammed || !shooter.conscious ||
                shooter.GetItem(shooter.handSlot) != it)
            {
                StopFiring();
                return;
            }
            if (fireMode == BurstFireMode && burstShotsRemaining > 0)
            {
                burstShotsRemaining--;
                if (!FireRound(shooter) || burstShotsRemaining == 0)
                    StopFiring();
                return;
            }
            if (burstShotsRemaining > 0 || fireMode != 3)
            {
                StopFiring();
                return;
            }
            if (shooter == PlayerCamera.main.body && !Input.GetKey(KeyBinds.GetBind("attack")))
            {
                StopFiring();
                return;
            }
            if (!FireRound(shooter)) StopFiring();
        }

        // Burst completes on a quick tap; full-auto stops as soon as the host receives release.
        public void ReleaseTrigger()
        {
            if (fireMode != BurstFireMode || burstShotsRemaining == 0)
                StopFiring();
        }

        private void StopFiring()
        {
            burstShotsRemaining = 0;
            shooter = null;
            currentlyShooting = false;
        }
    }
}
