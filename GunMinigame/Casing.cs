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
            casingImage.GetComponent<RectTransform>().sizeDelta = MinigameManager.size;
            casingImage.sprite = casing;
//            UnityEngine.Object.Destroy(casingImage.gameObject, 10f);
        }
    }
}
