using UnityEngine;

namespace NewFirearms
{
    public class LaserPointer : MonoBehaviour
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
            RaycastHit2D[] array = Physics2D.RaycastAll(transform.position, transform.right * 1f, LayerMask.GetMask("Ground", "Body", "Limb", "Descriptor"));
            RaycastHit2D[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                RaycastHit2D raycastHit2D = array2[i];
                if (raycastHit2D.collider != coll &&
                    !(raycastHit2D.collider.TryGetComponent<Body>(out Body body) && body.HoldingItem(it)) &&
                    !(raycastHit2D.collider.TryGetComponent<Limb>(out Limb limb) && limb.body.HoldingItem(it))
                    )
                {
                    UnityEngine.Debug.Log(raycastHit2D.collider.gameObject.name);
                    lr.SetPosition(1, new Vector2(raycastHit2D.distance / (1f + (1f - 1f) * 0.45f), 0f));
                    break;
                }
            }
            if (array.Length == 0)
            {
                lr.SetPosition(1, new Vector2(999f, 0f));
            }
        }

        public void OnDisable()
         => lr.enabled = false;

        public void OnEnable()
         => lr.enabled = true;
    }
}
