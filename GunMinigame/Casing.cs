using UnityEngine.UI;
using UnityEngine;

namespace GunMinigame
{
    public class Casing : MonoBehaviour
    {
        public static void Create(Sprite casing, Vector3 position)
        {
            UnityEngine.Debug.Log("lol");
            Image casingImage = new GameObject("Casing").AddComponent<Image>();
            casingImage.transform.SetParent(MinigameManager.GetOrAddInstance().maskBase.transform);
            casingImage.transform.localScale = Vector3.one;
            casingImage.transform.localPosition = position;
            casingImage.GetComponent<RectTransform>().sizeDelta = MinigameManager.size * 0.1f;
            casingImage.sprite = casing;
            casingImage.gameObject.AddComponent<Casing>();
            casingImage.raycastTarget = false;
            UnityEngine.Object.Destroy(casingImage.gameObject, 2f);
        }


        public Vector3 inertia;
        private void Update()
        {
            transform.localPosition += inertia * Time.deltaTime;
            inertia += new Vector3(0f, -20f * Time.deltaTime);
        }
    }
}
