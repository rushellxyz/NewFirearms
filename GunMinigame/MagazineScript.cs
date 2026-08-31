using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine;

namespace GunMinigame
{
    public class MagazineScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static Vector2 sensetivity;
        public Item it;

        private void Awake()
        {
            sensetivity = new Vector2((2.15f/2560f)*Screen.width, (2.15f/1440f)*Screen.height);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            MinigameManager.GetOrAddInstance().magazineDragTrigger.gameObject.SetActive(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.localPosition += (Vector3)(MinigameManager.GetOrAddInstance().handVelocity * sensetivity);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (MagazineDragTrigger.isHovering)
                MinigameManager.GetOrAddInstance().gun.DragOnto(it);
       else if (!Plugin.IsMarksman(PlayerCamera.main.body))
                it.transform.parent.GetComponent<Container>().UnloadItem(it);
            MinigameManager.GetOrAddInstance().magazineDragTrigger.gameObject.SetActive(false);
            var _ = ааа();
        }

        private async Task ааа()
        {
            await Task.Delay(100);
            MinigameManager.GetOrAddInstance().shouldUpdateMagazineCount = true;
        }
    }
}
