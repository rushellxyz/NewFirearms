namespace NewFirearms
{
    public partial class RshGun
    {
        internal uint SnapshotRevision;
        private bool hasReceivedSnapshot;
        private bool? requestedRackState;
        private float rackRequestTime;

        private bool RequestManualRackIfClient()
        {
            if (!Plugin.MultiplayerActive || Plugin.IsHost()) return false;
            // Rack is asynchronous on clients. Do not enqueue a toggle every UI frame.
            if (requestedRackState.HasValue && UnityEngine.Time.unscaledTime - rackRequestTime < 1.5f)
                return true;
            requestedRackState = !racked;
            rackRequestTime = UnityEngine.Time.unscaledTime;
            return RequestHostIfClient(1);
        }

        internal void ObserveRackSnapshot(bool state)
        {
            if (requestedRackState == state) requestedRackState = null;
        }


        internal sbyte ChamberSpentAmmo
        {
            get => spentAmmoTypes.TryGetValue(0, out sbyte ammo) ? ammo : (sbyte)-1;
            set
            {
                if (value >= 0 && value < prop.ammoTypes.Count && rounds.Count > 0 && rounds[0] == -1)
                    spentAmmoTypes[0] = value;
                else
                    spentAmmoTypes.Remove(0);
            }
        }

        internal bool AcceptSnapshot(uint revision)
        {
            if (hasReceivedSnapshot && unchecked((int)(revision - SnapshotRevision)) <= 0)
                return false;
            SnapshotRevision = revision;
            hasReceivedSnapshot = true;
            return true;
        }
    }
}
