using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        public const byte BurstFireMode = 4;
        private int burstShotsRemaining;
        private Vector2? clientShotPosition;
        private float nextShotStateUpdate;

        public bool Shoot(Body body = null)
            => Shoot(body, null);

        internal bool Shoot(Body body, Vector2? clientGunPosition, bool? clientSupportGrip = null)
        {
            if (Plugin.MultiplayerActive && RequestHostIfClient(0))
            {
                currentlyShooting = true;
                return true;
            }
            // Repeated requests cannot restart or extend a burst already in progress.
            if (burstShotsRemaining > 0 || gasTime > 0f) return false;
            if (clientSupportGrip.HasValue) ApplySupportGrip(body, clientSupportGrip.Value);
            clientShotPosition = !Plugin.sett.strictSync ? clientGunPosition : null;
            if (!FireRound(body))
            {
                StopFiring();
                return false;
            }
            if (fireMode == BurstFireMode && !IsJammed && !needsRetraction)
                burstShotsRemaining = 2; // The trigger pull already fired the first round.
            return true;
        }

        private void ContinueFiring()
        {
            if (shooter == null || IsJammed || !shooter.conscious ||
                (shooter.GetItem(0) != it && shooter.GetItem(1) != it))
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
            if (shooter == PlayerCamera.main.body && (!Input.GetKey(KeyBinds.GetBind("attack")) ||
                shooter.GetItem(shooter.handSlot) != it && (!Input.GetKey(Plugin.fireBothKey) ||
                    shooter.GetItem(shooter.handSlot)?.GetComponent<RshGun>() == null)))
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
            clientShotPosition = null;
            burstShotsRemaining = 0;
            shooter = null;
            currentlyShooting = false;
        }

        private Vector2 GetShotPosition()
        {
            Vector2 position = Plugin.MultiplayerActive && !Plugin.sett.strictSync && clientShotPosition.HasValue
                ? clientShotPosition.Value : (Vector2)transform.position;
            return position + (Vector2)transform.up * prop.barrelOffset;
        }

        internal void UpdateClientShotState(Body body, Vector2? position, bool supportGrip)
        {
            if (shooter != body) return;
            if (!Plugin.sett.strictSync && position.HasValue) clientShotPosition = position;
            ApplySupportGrip(body, supportGrip);
        }

        private void SendShotStateUpdate()
        {
            if (Plugin.sett.strictSync && prop?.minigame?.HasSupportGrip != true ||
                !currentlyShooting || fireMode != 3 && fireMode != BurstFireMode ||
                Time.unscaledTime < nextShotStateUpdate) return;
            nextShotStateUpdate = Time.unscaledTime + 0.05f;
            RequestHostIfClient(8);
        }
    }
}
