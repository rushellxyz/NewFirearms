using UnityEngine.EventSystems;
using UnityEngine;

namespace GunMinigame
{
    public class FannyPackScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static bool isHovering;

        public void OnPointerEnter(PointerEventData eventData)
        {
            UnityEngine.Debug.Log("Enter");
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UnityEngine.Debug.Log("Exit");
            isHovering = false;
        }
    }
}
