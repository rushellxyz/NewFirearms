using System.Runtime.Serialization;
using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun : GunMinigame.IMinigameRetractableGun
    {
        internal bool needsRetraction;
        private Vector2 tetherPoint;
        private Vector2Int? tetherBlock;
        private ushort tetherBlockId;

        internal Transform TetherTarget;
        internal Vector2 TetherLocalPoint;
        private LineRenderer tetherLine;

        public bool UsesRetraction() => prop?.feedType == FeedKind.Battery && prop.needRetract;

        // Прикольно что есть [OnSerializing], но я сомниваюсь что имено RetractBeforeSaving нужен
        [OnSerializing]
        private void RetractBeforeSaving(StreamingContext context)
        {
            if (!needsRetraction || Plugin.MultiplayerActive && !Plugin.IsHost()) return;
            StopFiring();
            ApplyTetherState(false, Vector2.zero);
            if (Plugin.MultiplayerActive) SyncIfHost();
        }

        internal Vector2 TetherPosition
        {
            get
            {
                if (TetherTarget != null)
                    tetherPoint = TetherTarget.TransformPoint(TetherLocalPoint);
                return tetherPoint;
            }
        }

        internal void AttachTether(Transform target, Vector2 point, Vector2Int? block = null)
        {
            if (needsRetraction) return;
            ApplyTetherState(true, point, target, block);
        }

        internal void ApplyTetherState(bool active, Vector2 point, Transform target = null, Vector2Int? block = null)
        {
            needsRetraction = active;
            tetherPoint = point;
            TetherTarget = active ? target : null;
            tetherBlock = active ? block : null;
            tetherBlockId = tetherBlock.HasValue ? WorldGeneration.world.GetBlock(tetherBlock.Value) : (ushort)0;
            TetherLocalPoint = target != null ? (Vector2)target.InverseTransformPoint(point) : Vector2.zero;
            if (!active) DestroyTetherVisual();
        }

        private void LateUpdate()
        {
            UpdateFlashlightVisual();
            if (!needsRetraction) return;
            // Unity compares destroyed objects equal to null, while their managed reference remains.
            bool targetDestroyed = TetherTarget == null && !ReferenceEquals(TetherTarget, null);
            if (tetherBlock.HasValue)
                targetDestroyed = WorldGeneration.world == null || tetherBlockId == 0 ||
                    WorldGeneration.world.GetBlock(tetherBlock.Value) != tetherBlockId;
            if (targetDestroyed &&
                (!Plugin.MultiplayerActive || Plugin.IsHost()))
            {
                StopFiring();
                ApplyTetherState(false, Vector2.zero);
                if (Plugin.MultiplayerActive) SyncIfHost();
                return;
            }
            Vector2 end = TetherPosition;
            if (Plugin.MultiplayerActive && Plugin.IsDedicated()) return;
            if (tetherLine == null)
            {
                var visual = (GameObject)Instantiate(Resources.Load("Special/TurretLine"), transform.position, Quaternion.identity);
                tetherLine = visual.GetComponent<LineRenderer>();
                tetherLine.useWorldSpace = true;
                tetherLine.positionCount = 2;
            }
            tetherLine.SetPosition(0, transform.position + transform.up * prop.barrelOffset);
            tetherLine.SetPosition(1, end);
        }

        private void DestroyTetherVisual()
        {
            if (tetherLine != null) Destroy(tetherLine.gameObject);
            tetherLine = null;
        }

        private void OnDisable()
        {
            halfRecoil = false;
            clientHalfRecoilBody = null;
            DestroyTetherVisual();
            if (gunFlashlight != null) gunFlashlight.enabled = false;
        }
    }
}
