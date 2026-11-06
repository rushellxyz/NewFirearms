namespace NewFirearms
{
    public partial class RshGun
    {
        internal uint SnapshotRevision;
        private bool hasReceivedSnapshot;

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
