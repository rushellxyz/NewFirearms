using UnityEngine;
using UnityEngine.EventSystems;

namespace GunMinigame
{
    public class SlidingMagazine : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private Vector3 previousPointer;
        private bool grabbed;

        public void OnBeginDrag(PointerEventData eventData)
        {
            Transform gun = MinigameManager.GetOrAddInstance().gunBase.transform;
            previousPointer = gun.InverseTransformPoint(eventData.position);
            grabbed = previousPointer.x >= gun.InverseTransformPoint(transform.position).x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!grabbed)
                return;
            MinigameManager manager = MinigameManager.GetOrAddInstance();
            Vector3 pointer = manager.gunBase.transform.InverseTransformPoint(eventData.position);
            manager.SlideStagedMagazine(pointer.y - previousPointer.y);
            previousPointer = pointer;
        }
    }
}
