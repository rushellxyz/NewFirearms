using UnityEngine;

namespace GunMinigame
{
    // A local UI adapter, never a replacement for the saved/networked GunScript.
    internal sealed class StandaloneGun : MonoBehaviour, IMinigameGun
    {
        private GunScript gun;
        private Item item;
        private MinigameInfo info;
        internal bool HalfRecoil { get; private set; }
        internal bool FiredDuringUpdate;

        internal void Initialize(GunScript source, Item sourceItem, MinigameInfo sourceInfo)
        {
            gun = source;
            item = sourceItem;
            info = sourceInfo;
        }

        internal bool IsLocallyHeld => gun != null && item != null && PlayerCamera.main != null &&
            PlayerCamera.main.body != null && PlayerCamera.main.body.GetItem(PlayerCamera.main.body.handSlot) == item;

        public bool IsRacked() => gun != null && gun.racked;
        public bool IsReady() => gun != null && !gun.safe && !gun.racked &&
            gun.roundInChamber == GunScript.RoundInChamber.Round;
        public byte CurrentFireMode() => gun == null || gun.safe ? (byte)0 : (byte)((int)gun.firingMode + 1);
        public string CurrentMag() => gun != null && gun.hasMag && gun.feedType == GunScript.FeedType.Mag
            ? AmmoScript.AmmoTypeToMagazine(gun.ammoType) : null;
        public bool HasEmptyMagazine() => !string.IsNullOrEmpty(CurrentMag()) && gun.roundsInMag <= 0;

        public bool TryGetChamberVisual(out Sprite sprite, out bool liveRound)
        {
            sprite = null;
            liveRound = gun != null && gun.roundInChamber == GunScript.RoundInChamber.Round;
            if (gun == null || gun.roundInChamber == GunScript.RoundInChamber.None || info?.casingsSprite == null)
                return false;
            int index = liveRound ? 1 : 0;
            if (index < info.casingsSprite.Length) sprite = info.casingsSprite[index];
            return sprite != null;
        }

        // Together patches the camera actions; preserve those entry points.
        public void Rack() { if (CanAct()) PlayerCamera.main.GunRack(); }
        public void RemoveMag() { if (CanAct()) PlayerCamera.main.GunEjectMag(); }
        // Vanilla supports a safety, not selectable NewFirearms firing modes.
        public void ToggleFireMode() { if (CanAct()) PlayerCamera.main.GunToggleSafety(); }

        public bool DragOnto(Item ammunition)
        {
            if (!CanAct() || ammunition == null || !ammunition.TryGetComponent<AmmoScript>(out var ammo) ||
                ammo.ammoType != gun.ammoType) return false;
            bool round = ammo.itemType == AmmoScript.AmmoItemType.Round;
            bool canLoad = round ? gun.racked && gun.roundInChamber == GunScript.RoundInChamber.None ||
                gun.feedType == GunScript.FeedType.Direct && gun.roundsInMag < gun.magCapacity
                : !gun.hasMag && gun.feedType == GunScript.FeedType.Mag;
            var body = PlayerCamera.main.body;
            if (!canLoad || !body.CanCombine(item, ammunition)) return false;
            body.CombineItems(item, ammunition);
            return true;
        }

        public void SetHalfRecoil(bool active) => HalfRecoil = active;

        internal void FilterTriggerInput()
        {
            var manager = MinigameManager.instance;
            if (!Plugin.useMinigame || !IsLocallyHeld || manager == null || !manager.active || manager.hiden ||
                !ReferenceEquals(manager.gun, this) || manager.uiImage == null || !manager.uiImage.isActiveAndEnabled)
                return;
            var canvas = manager.canvas != null ? manager.canvas.GetComponent<Canvas>() : null;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            // Release a trigger set on the preceding frame before vanilla Update can fire it.
            if (manager.IsManipulatingGun || RectTransformUtility.RectangleContainsScreenPoint(
                manager.uiImage.rectTransform, Input.mousePosition, camera))
                gun.triggerPressed = false;
        }

        private bool CanAct() => Plugin.standalone && Plugin.useMinigame && IsLocallyHeld &&
            PlayerCamera.main.body.conscious;
        private void OnDisable() => HalfRecoil = false;
    }
}
