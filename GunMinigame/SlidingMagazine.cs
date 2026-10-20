using UnityEngine;
using UnityEngine.EventSystems;

namespace GunMinigame
{
    public class SlidingMagazine : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private Vector3 previousPointer;

        public void OnBeginDrag(PointerEventData eventData)
        {
            previousPointer = transform.parent.InverseTransformPoint(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 pointer = transform.parent.InverseTransformPoint(eventData.position);
            MinigameManager.GetOrAddInstance().SlideStagedMagazine(pointer.x - previousPointer.x);
            previousPointer = pointer;
        }
    }
}
