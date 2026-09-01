using System;
using UnityEngine;

namespace NewFirearms
{
    public abstract class RshComponent : MonoBehaviour
    {
        //public static event Action OnDestroyEverything;

        public Item it;

        public MonoBehaviour mpTracker;

        private void Awake()
        {
            it = GetComponent<Item>();
            if (Plugin.useCuCore)
            {
                LoadPropFromCuCore();
                if (gameObject.TryGetComponent<GunScript>(out GunScript gs))
                    UnityEngine.Object.Destroy(gs);
                if (gameObject.TryGetComponent<AmmoScript>(out AmmoScript ass))
                    UnityEngine.Object.Destroy(ass);
            }
        }

        public abstract void LoadPropFromCuCore();

        // if it returns KrokoshaCasualtiesMP.KrokoshaScavMultiGameObjectNetworkTracker
        // then the entire game would crash on saving without mp mod dll
        // instead return monobehaviour and convert it on caller
        public MonoBehaviour GetMpTracker()
        {
            if (null != mpTracker)
                return mpTracker;
            mpTracker = GetComponent<KrokoshaCasualtiesMP.KrokoshaScavMultiGameObjectNetworkTracker>();
            if (null == mpTracker)
                throw new Exception("[NewFirearms] Attempt to action on item without KrokoshaScavMultiGameObjectNetworkTracker :hmm:");
            return mpTracker;
        }

        public static bool IsClient()
        {
            return KrokoshaCasualtiesMP.Net.is_client;
        }

        /*public static void DestroyEverything()
        {
            OnDestroyEverything?.Invoke();
        }

        private void OnEnable()
        {
            OnDestroyEverything += DestroyThis;
        }

        private void OnDisable()
        {
            OnDestroyEverything -= DestroyThis;
        }

        public void DestroyThis()
        {
            UnityEngine.Object.Destroy(gameObject);
        }*/
    }
}
