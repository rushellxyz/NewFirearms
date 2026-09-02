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
            MinigameManager mang = MinigameManager.GetOrAddInstance();
            mang.magazineDragTrigger.gameObject.SetActive(true);
            GetComponent<RectTransform>().SetParent(mang.handTransform);
            GetComponent<Image>().raycastTarget = false; // scary fix
        }

        public void OnDrag(PointerEventData eventData)
        {
            // ???
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            MinigameManager mang = MinigameManager.GetOrAddInstance();
            if (MagazineDragTrigger.isHovering)
                mang.gun.DragOnto(it);
       else if (!FannyPackScript.isHovering && !Plugin.IsMarksman(PlayerCamera.main.body))
                it.transform.parent.GetComponent<Container>().UnloadItem(it);
            mang.magazineDragTrigger.gameObject.SetActive(false);
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
