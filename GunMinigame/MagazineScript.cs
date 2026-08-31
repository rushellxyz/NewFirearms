using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace GunMinigame
{
    public class MagazineScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Item it;

        public void OnBeginDrag(PointerEventData eventData)
        {
            MinigameManager.GetOrAddInstance().magazineDragTrigger.gameObject.SetActive(true);
            GetComponent<RectTransform>().SetParent(MinigameManager.GetOrAddInstance().handTransform);
            GetComponent<Image>().raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // ???
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (MagazineDragTrigger.isHovering)
                MinigameManager.GetOrAddInstance().gun.DragOnto(it);
       else if (!Plugin.IsMarksman(PlayerCamera.main.body))
                it.transform.parent.GetComponent<Container>().UnloadItem(it);
            MinigameManager.GetOrAddInstance().magazineDragTrigger.gameObject.SetActive(false);
            var _ = ааа();
            UnityEngine.Object.Destroy(gameObject);
        }

        private async Task ааа()
        {
            await Task.Delay(100);
            MinigameManager.GetOrAddInstance().shouldUpdateMagazineCount = true;
        }
    }
}
