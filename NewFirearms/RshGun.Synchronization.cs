namespace NewFirearms
{
    public partial class RshGun
    {
        internal uint SnapshotRevision;
        private bool hasReceivedSnapshot;

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
