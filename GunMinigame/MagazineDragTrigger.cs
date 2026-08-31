using UnityEngine.EventSystems;
using UnityEngine;

namespace GunMinigame
{
    public class MagazineDragTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static bool isHovering;

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }
    }
}
