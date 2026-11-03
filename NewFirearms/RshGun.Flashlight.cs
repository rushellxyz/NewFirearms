using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NewFirearms
{
    public partial class RshGun : GunMinigame.IMinigameFlashlight
    {
        // Runtime-only: lights start switched off, including after loading a save.
        internal bool FlashlightEnabled;
        private Light2D gunFlashlight;
        private bool flashlightResourceMissing;
        private static ItemInfo flashlightDefinition;

        public bool HasFlashlight() => prop?.feedType == BatteryFeed && prop.hasFlashlight;

        private bool HasFlashlightPower => !string.IsNullOrEmpty(mag) && BatteryCells.Charge(magazineBatteries) > 0f;

        public void ToggleFlashlight()
        {
            if (!HasFlashlight()) return;
            if (Plugin.MultiplayerActive && RequestHostIfClient(7)) return;
            if (!FlashlightEnabled && !HasFlashlightPower) return;
            FlashlightEnabled = !FlashlightEnabled;
            Sound.Play("flashlighttoggle", transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            if (Plugin.MultiplayerActive) SyncIfHost(7);
        }

        private void UpdateFlashlight()
        {
            if (!HasFlashlight() || !FlashlightEnabled || Plugin.MultiplayerActive && !Plugin.IsHost()) return;
            if (HasFlashlightPower)
            {
                bool couldShoot = HasBatteryPower;
                // Stock low mode: decayMultiplier .5, Small preset .5, and 50 charge per cell.
                if (flashlightDefinition == null) flashlightDefinition = Item.GetItem("flashlight");
                float drain = flashlightDefinition.rotSpeed * 0.005f * WorldGeneration.globalDecayRate *
                    (it.isWet ? 6f : 1f) * Time.deltaTime;
                BatteryCells.Drain(magazineBatteries, drain);
                UpdateBatteryRound();
                if (couldShoot != HasBatteryPower) UpdateSprite();
            }
            if (!HasFlashlightPower)
            {
                FlashlightEnabled = false;
                if (Plugin.MultiplayerActive) SyncIfHost();
            }
        }

        private void UpdateFlashlightVisual()
        {
            bool lit = HasFlashlight() && FlashlightEnabled && HasFlashlightPower && it.ParentContainer() == null;
            if (!lit || Plugin.MultiplayerActive && Plugin.IsDedicated())
            {
                if (gunFlashlight != null) gunFlashlight.enabled = false;
                return;
            }
            if (gunFlashlight == null)
            {
                if (flashlightResourceMissing) return;
                var prefab = Resources.Load<GameObject>("emergencylight");
                var source = prefab != null ? prefab.GetComponentInChildren<Light2D>(true) : null;
                if (source == null)
                {
                    flashlightResourceMissing = true;
                    Debug.LogWarning("[NewFirearms] Emergency light prefab has no Light2D; gun flashlight visual unavailable.");
                    return;
                }
                // Clone only the light child, without the emergency light's item/use/decay scripts.
                gunFlashlight = Instantiate(source, transform);
                gunFlashlight.transform.localPosition = new Vector3(0f, prop.barrelOffset, 0f);
                gunFlashlight.intensity = 0.5f;
                gunFlashlight.gameObject.SetActive(true);
            }
            bool left = transform.parent != null && transform.parent.TryGetComponent<InventorySlot>(out var slot) && !slot.body.isRight;
            // Match the shot direction in world space, accounting for the held gun's mirrored scale.
            Vector3 direction = transform.InverseTransformVector(transform.right * (left ? -1f : 1f));
            gunFlashlight.transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            gunFlashlight.enabled = true;
        }
    }
}
