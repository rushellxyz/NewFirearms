using UnityEngine;

namespace NewFirearms
{
    // A course-only target, handled explicitly by ShootManager rather than game-wide patches.
    public class FirearmCourseTarget : MonoBehaviour
    {
        private RshGun requiredGun;
        private float health = 60f;
        private Texture2D texture;
        private Sprite sprite;
        public bool Destroyed => health <= 0f;

        public static FirearmCourseTarget Create(Vector2 position, RshGun gun)
        {
            var obj = new GameObject("Firearm course dummy");
            obj.layer = LayerMask.NameToLayer("Descriptor");
            obj.transform.position = position;
            var target = obj.AddComponent<FirearmCourseTarget>();
            target.requiredGun = gun;
            target.texture = new Texture2D(16, 32, TextureFormat.RGBA32, false);
            target.texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 16; x++)
                {
                    bool head = (x - 7.5f) * (x - 7.5f) + (y - 26f) * (y - 26f) <= 20f;
                    bool torso = y >= 8 && y < 22 && x >= 2 && x <= 13;
                    bool legs = y < 8 && (x >= 3 && x <= 6 || x >= 9 && x <= 12);
                    bool bullseye = (x - 7.5f) * (x - 7.5f) + (y - 16f) * (y - 16f) < 12f;
                    target.texture.SetPixel(x, y, head || torso || legs
                        ? bullseye ? Color.red : new Color(0.85f, 0.75f, 0.5f) : Color.clear);
                }
            target.texture.Apply();
            target.sprite = Sprite.Create(target.texture, new Rect(0f, 0f, 16f, 32f), new Vector2(0.5f, 0.5f), 12f);
            obj.AddComponent<SpriteRenderer>().sprite = target.sprite;
            obj.AddComponent<BoxCollider2D>().size = new Vector2(1.3f, 2.7f);
            return target;
        }

        public void Hit(Body shooter, float damage)
        {
            if (Destroyed || requiredGun == null || shooter == null ||
                !shooter.HoldingItem(shooter.handSlot) || shooter.GetItem(shooter.handSlot) != requiredGun.it) return;
            health -= Mathf.Max(1f, damage);
            if (Destroyed)
            {
                GetComponent<Collider2D>().enabled = false;
                GetComponent<SpriteRenderer>().color = Color.gray;
                transform.rotation = Quaternion.Euler(0f, 0f, 80f);
            }
        }

        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
