using UnityEngine;

namespace NewFirearms
{
    public class LaserPointer : MonoBehaviour, GunMinigame.ILaser
    {
        public Item it;
        public LineRenderer lr;
        public Collider2D coll;

        public void Awake()
        {
            coll = GetComponent<Collider2D>();
            it = GetComponent<Item>();
            lr = gameObject.AddComponent<LineRenderer>();

            lr.useWorldSpace = false;
            lr.startColor = Color.red;
            lr.endColor = Color.red;

            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;

            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(0f,0f,-1f));

            // Воруем материал у глючного орба
            lr.material = Observer.main.GetComponent<SpriteRenderer>().material;
        }

        public void Update()
        {
            float dir = 1f;
            // Предметы на земле всегда смотрят на право
            // Теперь ты никогда это не развидишь
            if (null != transform.parent && transform.parent.TryGetComponent<InventorySlot>(out InventorySlot slot) && !slot.body.isRight)
                dir = -1f;

            RaycastHit2D[] array = Physics2D.RaycastAll(transform.position, transform.right * dir, 200f, LayerMask.GetMask("Ground", "Body", "Limb", "Descriptor"));
            RaycastHit2D[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                RaycastHit2D raycastHit2D = array2[i];
                if ( raycastHit2D.collider != coll &&
                   !(raycastHit2D.collider.TryGetComponent<Body>(out Body body) && body.HoldingItem(it)) &&
                   !(raycastHit2D.collider.TryGetComponent<Limb>(out Limb limb) && limb.body.HoldingItem(it) &&
                   (
                    (raycastHit2D.collider.TryGetComponent<BuildingEntity>(out var component) && !component.cantHit) ||
                    (raycastHit2D.collider.gameObject.layer == 6) ||
                    ((bool)raycastHit2D.rigidbody)
                   )))
                {
                    lr.SetPosition(1, new Vector2(raycastHit2D.distance / (1f + (1f - 1f) * 0.45f), 0f));
                    break;
                }
            }
            if (array.Length == 0)
            {
                lr.SetPosition(1, new Vector2(200f, 0f));
            }
        }

        public void Toggle()
         => this.enabled = !this.enabled;

        public bool IsEnabled()
         => this.enabled;

        public void OnDisable()
         => lr.enabled = false;

        public void OnEnable()
         => lr.enabled = true;
    }
}
