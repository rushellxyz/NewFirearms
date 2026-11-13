using System.Runtime.CompilerServices;
using UnityEngine;

namespace NewFirearms
{
    public partial class RshGun
    {
        internal static bool IsPiggybackAiming(Body body)
            => Plugin.MultiplayerActive && body != null && !body.standing && IsOnBack(body);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsOnBack(Body body)
            => body.TryGetComponent<Together.NetBody>(out var netBody) && netBody.piggybacking_on != null;

        public void FixedUpdate()
        {
            if (!Plugin.MultiplayerActive || transform.parent == null ||
                !transform.parent.TryGetComponent<InventorySlot>(out var slot) ||
                slot.body == null || slot.slot != slot.body.handSlot || !IsPiggybackAiming(slot.body))
                return;
            Limb hand = slot.limb;
            if (hand == null || hand.rb == null || !hand.rb.simulated) return;
            Vector2 direction = (Vector2)slot.body.targetLookPos - hand.rb.position;
            if (direction.sqrMagnitude <= 2f) return;

            // Together's carried player is a ragdoll: aim the physical arm chain.
            float facing = slot.body.isRight ? 1f : -1f;
            float targetAngle = Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;
            float difference = Mathf.DeltaAngle(hand.rb.rotation, targetAngle);
            float speed = 400f * Time.fixedDeltaTime * hand.totalForce * Mathf.Clamp01(1f - gasTime * 4f);
            hand.rb.MoveRotation(Mathf.MoveTowardsAngle(hand.rb.rotation, targetAngle, speed * (30f + Mathf.Abs(difference))));
            Limb wrist = ConnectedArmLimb(hand);
            if (wrist == null) return;
            if (wrist.rb != null && wrist.rb.simulated)
                wrist.rb.MoveRotation(Mathf.MoveTowardsAngle(wrist.rb.rotation, targetAngle + 20f * facing, speed * 8f));
            Limb arm = ConnectedArmLimb(wrist);
            if (arm != null && arm.rb != null && arm.rb.simulated)
                arm.rb.MoveRotation(Mathf.MoveTowardsAngle(arm.rb.rotation, targetAngle - 20f * facing, speed * 12f));
        }

        private static Limb ConnectedArmLimb(Limb limb)
            => limb.connectedLimbs != null && limb.connectedLimbs.Length > 0 ? limb.connectedLimbs[0] : null;
    }
}
