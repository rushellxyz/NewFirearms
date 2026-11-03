using System.Runtime.Serialization;
using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun : GunMinigame.IMinigameRetractableGun
    {
        internal bool needsRetraction;
        private Vector2 tetherPoint;
        internal uint TetherRevision;

        internal Transform TetherTarget;
        internal Vector2 TetherLocalPoint;
        private LineRenderer tetherLine;

        public bool UsesRetraction() => prop?.feedType == BatteryFeed && prop.needRetract;

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

        internal void AttachTether(Transform target, Vector2 point)
        {
            if (needsRetraction) return;
            ApplyTetherState(true, point, target);
        }

        internal void ApplyTetherState(bool active, Vector2 point, Transform target = null)
        {
            if (!Plugin.MultiplayerActive || Plugin.IsHost()) TetherRevision++;
            needsRetraction = active;
            tetherPoint = point;
            TetherTarget = active ? target : null;
            TetherLocalPoint = target != null ? (Vector2)target.InverseTransformPoint(point) : Vector2.zero;
            if (!active) DestroyTetherVisual();
        }

        private void LateUpdate()
        {
            if (!needsRetraction) return;
            // Unity compares destroyed objects equal to null, while their managed reference remains.
            if (TetherTarget == null && !ReferenceEquals(TetherTarget, null) &&
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

        private void OnDisable() => DestroyTetherVisual();
    }
}
