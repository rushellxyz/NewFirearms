using UnityEngine.UI;
using UnityEngine;

namespace GunMinigame
{
    public class Casing : MonoBehaviour
    {
        public static void Create(Sprite casing)
        {
            UnityEngine.Debug.Log("lol");
            Image casingImage = new GameObject("Casing").AddComponent<Image>();
            casingImage.transform.SetParent(MinigameManager.GetOrAddInstance().maskBase.transform);
            casingImage.transform.localPosition = Vector3.zero;
            casingImage.transform.localScale = Vector3.one;
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
            inertia += new Vector3(0f, -9.81f * Time.deltaTime);
        }
    }
}
