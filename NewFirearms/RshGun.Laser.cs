namespace NewFirearms
{
    public partial class RshGun
    {
        public void SetLaserEnabled(bool state)
        {
            if (prop?.hasLaser != true) return;
            if (Plugin.MultiplayerActive && RequestHostIfClient(9, laserState: state)) return;
            ApplyLaserState(state);
            if (Plugin.MultiplayerActive) SyncIfHost();
        }

        internal void ApplyLaserState(bool state)
        {
            laserEnabled = state;
            if (TryGetComponent<LaserPointer>(out var laser)) laser.enabled = state;
        }
    }
}
