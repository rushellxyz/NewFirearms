using UnityEngine;

namespace GunMinigame
{
    internal sealed class StandaloneMagazine : MonoBehaviour, IMinigameMag
    {
        internal static IMinigameMag For(Item item)
        {
            if (!Plugin.standalone || item == null || !item.TryGetComponent<AmmoScript>(out var ammo) ||
                ammo.itemType != AmmoScript.AmmoItemType.Magazine) return null;
            if (!item.TryGetComponent<StandaloneMagazine>(out var adapter))
                adapter = item.gameObject.AddComponent<StandaloneMagazine>();
            return adapter;
        }

        public bool DragOnto(Item ammunition)
        {
            var body = PlayerCamera.main != null ? PlayerCamera.main.body : null;
            var target = GetComponent<Item>();
            var magazine = GetComponent<AmmoScript>();
            if (!Plugin.standalone || !Plugin.useMinigame || body == null || !body.conscious ||
                target == null || magazine == null || ammunition == null ||
                !ammunition.TryGetComponent<AmmoScript>(out var round) ||
                round.itemType != AmmoScript.AmmoItemType.Round || round.ammoType != magazine.ammoType ||
                magazine.rounds >= magazine.maxRounds || !body.CanCombine(target, ammunition)) return false;
            body.CombineItems(target, ammunition);
            return true;
        }
    }
}
