using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace GunMinigame
{
    public class MagazineScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler
    {
        public Item it;
        public static MagazineScript currentlyHovering;
        private Container dragSource;

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragSource = it != null ? it.ParentContainer() : null;
            if (dragSource == null) return;

            MinigameManager mang = MinigameManager.GetOrAddInstance();
            MagazineDragTrigger.isHovering = false;
            mang.magazineDragTrigger.gameObject.SetActive(true);
            GetComponent<RectTransform>().SetParent(mang.handTransform);
            GetComponent<Image>().raycastTarget = false; // scary fix
        }

        public void OnDrag(PointerEventData eventData)
        {
            // ???
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            currentlyHovering = this;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            MinigameManager mang = MinigameManager.GetOrAddInstance();
            if (!mang.magazineDragTrigger.gameObject.activeSelf) return;

            if (dragSource != null && it != null && it.ParentContainer() == dragSource)
            {
                if (MagazineDragTrigger.isHovering)
                    mang.DropMagazine(it, GetComponent<Image>());
                else if (!FannyPackScript.isHovering && !Plugin.IsMarksman(PlayerCamera.main.body))
                    dragSource.UnloadItem(it);
            }
            dragSource = null;
            mang.EndMagazineDrag();
            UnityEngine.Object.Destroy(gameObject);
        }

        public void Jump()
        {
            StartCoroutine(_Jump());
        }

        private IEnumerator _Jump()
        {
            float timer = 0.2f;
            Vector3 ogPosition = transform.position;
            while (0.0f < timer)
            {
                transform.position -= new Vector3(0f, (0.1f - timer) * 20f);
                timer -= Time.deltaTime;
                yield return null;
            }
            transform.position = ogPosition;
        }
    }
}
