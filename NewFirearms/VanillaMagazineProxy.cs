using UnityEngine;

namespace NewFirearms
{
    // Vanilla magazine prefabs also exist on peers without the mod. Project the
    // authoritative count into AmmoScript so Together can synchronize their UI.
    internal sealed class VanillaMagazineProxy : MonoBehaviour
    {
        private RshMag magazine;
        private AmmoScript native;

        internal static void Attach(RshMag magazine)
        {
            if (!Plugin.nativeClientCompatibilityAvailable) return;
            var native = magazine.GetComponent<AmmoScript>();
            if (native == null || magazine.prop.type == MagazineKind.Battery) return;
            var proxy = magazine.GetComponent<VanillaMagazineProxy>() ?? magazine.gameObject.AddComponent<VanillaMagazineProxy>();
            proxy.magazine = magazine;
            proxy.native = native;
            native.enabled = false;
            proxy.LateUpdate();
        }

        private void LateUpdate()
        {
            if (magazine == null || native == null || magazine.prop == null) return;
            int rounds = magazine.TotalRounds();
            bool changed = native.rounds != rounds;
            native.ammoType = VanillaGunProxy.GetAmmoType(magazine.prop.ammoTypes[0]);
            native.itemType = AmmoScript.AmmoItemType.Magazine;
            native.maxRounds = magazine.prop.capacity;
            native.rounds = rounds;
            if (changed && Plugin.MultiplayerActive && Plugin.IsHost())
                VanillaClientNetwork.PublishState(gameObject);
        }
    }
}
