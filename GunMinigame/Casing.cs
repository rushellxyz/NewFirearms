using UnityEngine.UI;
using UnityEngine;

namespace GunMinigame
{
    public class Casing : MonoBehaviour
    {
        public Vector3 inertia;
        float spin, age;
        Image visual;
        MinigameManager owner;
        static Sprite fallback;

        public static void Create(Sprite casing, Vector3 position)
        {
            CreateVisual(MinigameManager.GetOrAddInstance(), casing, position, Vector3.zero, false);
        }

        internal static void CreateVisual(MinigameManager manager, Sprite sprite, Vector3 position, Vector3 inherited, bool live)
        {
            if (manager.maskBase == null || !manager.active) return;
            var existing = manager.maskBase.GetComponentsInChildren<Casing>();
            if (existing.Length >= 40) Destroy(existing[0].gameObject);
            if (sprite == null || sprite.texture == null || sprite.rect.width < 3 || sprite.rect.height < 3)
            {
                if (fallback == null)
                {
                    var texture = new Texture2D(5, 12, TextureFormat.RGBA32, false);
                    texture.filterMode = FilterMode.Point;
                    for (int y = 0; y < 12; y++)
                        for (int x = 0; x < 5; x++)
                            texture.SetPixel(x, y, x == 0 || x == 4 || y == 0 ? new Color(0.95f, 0.85f, 0.57f) : new Color(0.61f, 0.47f, 0.23f));
                    texture.Apply();
                    fallback = Sprite.Create(texture, new Rect(0, 0, 5, 12), new Vector2(0.5f, 0.5f));
                }
                sprite = fallback;
            }
            var image = new GameObject("Casing").AddComponent<Image>();
            image.transform.SetParent(manager.maskBase.transform, false);
            image.transform.localPosition = position;
            image.transform.localScale = new Vector3((9f/2560f)*Screen.width, (9f/1440f)*Screen.height);
            image.rectTransform.sizeDelta = live ? new Vector2(12, 26) : new Vector2(9, 19);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var casing = image.gameObject.AddComponent<Casing>();
            casing.visual = image;
            casing.owner = manager;
            casing.inertia = Vector3.ClampMagnitude(inherited, 400) + new Vector3(Random.Range(-190f, -110f), Random.Range(210f, 290f));
            casing.spin = Random.Range(-620f, 620f);
        }

        void Update()
        {
            if (owner == null || !owner.active) { Destroy(gameObject); return; }
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            if (PauseHandler.main != null && PauseHandler.main.isPaused) return;
            const float drag = 0.5f, gravity = 650f;
            float decay = Mathf.Exp(-drag * dt), integral = (1f - decay) / drag;
            Vector3 terminal = new Vector3(0, -gravity / drag);
            transform.localPosition += terminal * dt + (inertia - terminal) * integral;
            inertia = terminal + (inertia - terminal) * decay;
            transform.Rotate(0, 0, spin * dt);
            age += dt;
            if (visual != null) visual.color = new Color(1, 1, 1, owner.windowAlpha * Mathf.Clamp01((1.8f - age) / 0.3f));
            if (age >= 1.8f || transform.localPosition.y < -240) Destroy(gameObject);
        }
    }
}
