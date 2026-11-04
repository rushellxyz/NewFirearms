using UnityEngine;

namespace GunMinigame
{
    // A rigid rod: forces at the grip and muzzle produce translation and torque,
    // while the distance between them stays fixed. Units are fractions of window width.
    internal sealed class GunInertia
    {
        internal Vector2 Grip { get; private set; }
        internal Vector2 Muzzle { get; private set; }
        internal Vector2 Offset { get; private set; }
        internal Vector2 Velocity { get; private set; }
        internal float Angle { get; private set; }
        internal float AngularVelocity { get; private set; }
        internal float Mass { get; private set; } = 1f;

        private Vector2 center, gripArm, muzzleArm;
        private float moment;
        private bool sliderHeld;
        private Vector2 sliderPoint, sliderTarget;
        private const float MaxAngle = 20f * Mathf.Deg2Rad;

        internal GunInertia()
        {
            Configure(new Vector2(-0.3f, -0.025f), new Vector2(0.2f, 0.05f), 1f);
        }

        internal void Configure(Vector2 grip, Vector2 muzzle, float mass)
        {
            Grip = grip;
            Muzzle = muzzle;
            Mass = Mathf.Clamp(mass, 0.25f, 12f);
            center = (grip + muzzle) * 0.5f;
            gripArm = grip - center;
            muzzleArm = muzzle - center;
            moment = Mass * Mathf.Max((muzzle - grip).sqrMagnitude, 0.01f) / 12f;
            Reset();
        }

        internal void Reset()
        {
            Offset = Velocity = Vector2.zero;
            Angle = AngularVelocity = 0f;
            ReleaseSlider();
        }

        internal Vector2 GripPosition => center + Offset + Rotate(gripArm, Angle);
        internal Vector2 MuzzlePosition => center + Offset + Rotate(muzzleArm, Angle);
        internal Vector2 Position => center + Offset - Rotate(center, Angle);

        internal void ImpulseAtGrip(Vector2 impulse) => ApplyImpulse(gripArm, impulse);
        internal void ImpulseAtMuzzle(Vector2 impulse) => ApplyImpulse(muzzleArm, impulse);
        internal void ImpulseAtPoint(Vector2 point, Vector2 impulse) => ApplyImpulse(point - center, impulse);

        internal void HoldSlider(Vector2 point, Vector2 target)
        {
            sliderHeld = true;
            sliderPoint = point;
            sliderTarget = target;
        }

        internal void ReleaseSlider() => sliderHeld = false;

        private void ApplyImpulse(Vector2 arm, Vector2 impulse)
        {
            Velocity = Vector2.ClampMagnitude(Velocity + impulse / Mass, 4f);
            AngularVelocity = Mathf.Clamp(AngularVelocity + Cross(Rotate(arm, Angle), impulse) / moment, -12f, 12f);
        }

        internal void Step(float deltaTime, Vector2 handTarget, float gripStrength)
        {
            if (deltaTime <= 0f) return;
            float dt = Mathf.Min(deltaTime, 0.25f);
            int steps = Mathf.CeilToInt(dt * 240f);
            float h = dt / steps;
            gripStrength = Mathf.Clamp(gripStrength, 0.15f, 2f);
            handTarget = Vector2.ClampMagnitude(handTarget, 0.15f);
            for (int i = 0; i < steps; i++)
            {
                Vector2 grip = Rotate(gripArm, Angle);
                Vector2 muzzle = Rotate(muzzleArm, Angle);
                Vector2 gripVelocity = Velocity + Perpendicular(grip) * AngularVelocity;
                Vector2 muzzleVelocity = Velocity + Perpendicular(muzzle) * AngularVelocity;
                // The hand supplies most of the restoring force. The muzzle has weaker
                // support, allowing it to lag behind the hand and kick ahead on firing.
                Vector2 gripForce = ((Grip + handTarget - center - Offset - grip) * (240f * gripStrength) -
                    gripVelocity * (16f * Mathf.Sqrt(gripStrength))) * Mass;
                Vector2 muzzleForce = ((Muzzle - center - Offset - muzzle) * 55f - muzzleVelocity * 3f) * Mass;
                Vector2 totalForce = gripForce + muzzleForce;
                float torque = Cross(grip, gripForce) + Cross(muzzle, muzzleForce);
                if (sliderHeld)
                {
                    Vector2 arm = Rotate(sliderPoint - center, Angle);
                    Vector2 pointVelocity = Velocity + Perpendicular(arm) * AngularVelocity;
                    Vector2 stretch = sliderTarget - (center + Offset + arm);
                    // Unlike a movement impulse, this force persists at a travel stop.
                    Vector2 pull = Vector2.ClampMagnitude(stretch * 480f - pointVelocity * 28f, 25f);
                    totalForce += pull;
                    torque += Cross(arm, pull);
                }
                Velocity += totalForce / Mass * h;
                AngularVelocity += torque / moment * h;
                Offset += Velocity * h;
                Angle += AngularVelocity * h;
                LimitPose();
            }
        }

        private void LimitPose()
        {
            Vector2 limited = new Vector2(Mathf.Clamp(Offset.x, -0.15f, 0.15f), Mathf.Clamp(Offset.y, -0.12f, 0.12f));
            Velocity = new Vector2(limited.x != Offset.x ? 0f : Velocity.x, limited.y != Offset.y ? 0f : Velocity.y);
            Offset = limited;
            if (Mathf.Abs(Angle) > MaxAngle)
            {
                Angle = Mathf.Clamp(Angle, -MaxAngle, MaxAngle);
                AngularVelocity = 0f;
            }
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x);
        private static Vector2 Rotate(Vector2 v, float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }
    }
}
