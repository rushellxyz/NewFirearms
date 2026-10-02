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

        public void OnPointerEnter(PointerEventData eventData)
        {
            currentlyHovering = this;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            MinigameManager mang = MinigameManager.GetOrAddInstance();
            if (MagazineDragTrigger.isHovering)
                mang.gun.DragOnto(it);
       else if (!FannyPackScript.isHovering && !Plugin.IsMarksman(PlayerCamera.main.body))
                it.transform.parent.GetComponent<Container>().UnloadItem(it);
            mang.magazineDragTrigger.gameObject.SetActive(false);
            mang.DelayUpdateMagazineCount();
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
