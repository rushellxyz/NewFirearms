using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        private int FindTubeLoadSlot()
        {
            if (rounds == null) return -1;
            for (int slot = racked ? 0 : 1; slot < rounds.Count; slot++)
                if (rounds[slot] == -2)
                    return slot;
            return -1;
        }

        internal bool CanLoadTubeFromMagazine(RshMag source, Body actor)
            => TryGetTubeLoad(source, actor, out _, out _);

        private bool TryGetTubeLoad(RshMag source, Body actor, out int slot, out int ammoIndex)
        {
            slot = -1;
            ammoIndex = -1;
            if (actor == null || !GunMinigame.Plugin.IsMarksman(actor) || IsJammed ||
                prop?.feedType != FeedKind.Tube || source.prop?.type != MagazineKind.Regular ||
                source.rounds == null || source.rounds.Count == 0)
                return false;

            int sourceIndex = source.rounds[0];
            if (sourceIndex < 0 || sourceIndex >= source.prop.ammoTypes.Count)
                return false;
            string roundId = source.prop.ammoTypes[sourceIndex];
            ammoIndex = prop.ammoTypes.FindIndex(ammo => ammo.round == roundId);
            slot = FindTubeLoadSlot();
            return ammoIndex >= 0 && ammoIndex <= sbyte.MaxValue && slot >= 0;
        }

        private bool LoadTubeFromMagazine(Item item, RshMag source, Body actor)
        {
            if (!TryGetTubeLoad(source, actor, out int slot, out int ammoIndex)) return false;
            if (Plugin.MultiplayerActive && RequestHostIfClient(2, item)) return true;

            rounds[slot] = (sbyte)ammoIndex;
            source.RemoveTopRound();
            source.UpdateSprite();
            if (Plugin.MultiplayerActive) source.SyncIfHost();
            FinishLoadingRound(actor);
            return true;
        }

        private void FinishLoadingRound(Body actor, Item looseRound = null)
        {
            Sound.Play(prop.loadRoundAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            if (looseRound != null)
                UnityEngine.Object.Destroy(looseRound.gameObject);
            UpdateSprite();
            if (Plugin.MultiplayerActive) SyncIfHost(4);
            NotifyLoadHandling(actor, magazine: false);
        }
    }
}
