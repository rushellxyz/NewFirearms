using UnityEngine.EventSystems;
using UnityEngine;

namespace GunMinigame
{
    public class FannyPackScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
