using UnityEngine.EventSystems;
using UnityEngine;

namespace GunMinigame
{
    public class MagazineScript : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            transform.localPosition += (Vector3)MinigameManager.GetOrAddInstance().handVelocity;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            UnityEngine.Debug.Log("OnEndDrag");
        }
    }
}