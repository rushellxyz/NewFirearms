using UnityEngine;

namespace GunMinigame
{
    public partial class MinigameManager
    {
        private readonly GunInertia inertia = new GunInertia();
        private Body inertiaBody;
        private int inertiaSlot = -1;
        private Rigidbody2D inertiaHand;
        private Vector2 lastBodyVelocity, lastHandVelocity, lastBodyPosition;
        private float lastBodyAngularVelocity;
        private Vector2 painTremor;
        private bool sliderGripActive;
        private bool chargingHandleReturnPending;
        private bool chargingHandleCloseRequested;
        private Vector2 sliderGripLocalPoint;
        private Vector2 sliderPullTarget;
        private Quaternion sliderHandRotationOffset, sliderHandRestRotation;
        internal Vector2 SliderPointer => sliderGripActive ? sliderPullTarget : handPos;

        private void ResetInertia()
        {
            ResetFannyPackZip();
            magazineReleasedAtRear = false;
            chargingHandleReturnPending = false;
            chargingHandleCloseRequested = false;
            ResetOutsidePump();
            EndSupportGrip();
            ReleaseSliderPull();
            float aspect = size.x > 0f ? size.y / size.x : 0.5f;
            var component = gun as Component;
            var item = component != null ? component.GetComponent<Item>() : null;
            float mass = item != null && item.Stats != null ? item.Stats.weight : 1f;
            if (info != null)
                inertia.Configure(new Vector2(info.inertiaHandXPosition, info.inertiaHandYPosition * aspect),
                    new Vector2(info.inertiaBarrelXPosition, info.inertiaBarrelYPosition * aspect), mass);
            else inertia.Reset();
            inertiaBody = null;
            inertiaSlot = -1;
            inertiaHand = null;
            holdsSlide = false;
            painTremor = Vector2.zero;
            if (gunBase != null)
            {
                gunBase.transform.localPosition = Vector3.zero;
                gunBase.transform.localRotation = Quaternion.identity;
            }
        }

        public void AddHandlingImpulse(float horizontal, float vertical)
        {
            if (!active) return;
            inertia.ImpulseAtGrip(new Vector2(-horizontal * 0.025f, vertical * 0.02f));
        }

        public void AddFootstepImpulse(float volume)
        {
            if (!active || body == null) return;
            float side = Mathf.Abs(body.rb.velocity.x) > 0.1f ? Mathf.Sign(body.rb.velocity.x) : 0f;
            inertia.ImpulseAtGrip(new Vector2(-side * 0.012f, -0.035f) * Mathf.Clamp(volume, 0f, 3f) * inertia.Mass);
            zipStepDirection = -zipStepDirection;
            AddFannyPackZipImpulse(zipStepDirection * Mathf.Clamp(volume, 0f, 3f) * 18f);
        }

        internal void BeginSliderPull()
        {
            sliderGripLocalPoint = sliderFrontImage.transform.InverseTransformPoint(handPos);
            sliderPullTarget = handPos;
            sliderHandRestRotation = handTransform.localRotation;
            sliderHandRotationOffset = Quaternion.Inverse(sliderFrontImage.transform.rotation) * handTransform.rotation;
            sliderGripActive = true;
        }

        internal void ReleaseSliderPull()
        {
            if (sliderGripActive)
            {
                handVelocity = Vector2.zero;
                if (handTransform != null) handTransform.localRotation = sliderHandRestRotation;
            }
            sliderGripActive = false;
            inertia.ReleaseSlider();
        }

        private void AttachHandToSlider()
        {
            if (!holdsSlide || !sliderGripActive || automatedHandAction) return;
            handPos = sliderFrontImage.transform.TransformPoint(sliderGripLocalPoint);
            handTransform.position = handPos;
            //handTransform.rotation = sliderFrontImage.transform.rotation * sliderHandRotationOffset;
        }

        internal Vector2 SliderGripPoint => gunBase.transform.InverseTransformPoint(
            sliderFrontImage.transform.TransformPoint(sliderGripActive ? sliderGripLocalPoint : Vector2.zero));

        internal void AddSliderImpulse(Vector2 pixels)
        {
            Vector2 impulse = maskBase.transform.InverseTransformVector(gunBase.transform.TransformVector(pixels));
            inertia.ImpulseAtPoint(SliderGripPoint / Mathf.Max(size.x, 1f), impulse / Mathf.Max(size.x, 1f) * 1.5f);
        }

        private void UpdateSliderPull()
        {
            if (!holdsSlide || automatedHandAction || IsAnyGameUIOpen())
            {
                ReleaseSliderPull();
                return;
            }
            if (!sliderGripActive) BeginSliderPull();
            Vector2 target = maskBase.transform.InverseTransformPoint(sliderPullTarget);
            inertia.HoldSlider(SliderGripPoint / Mathf.Max(size.x, 1f), target / Mathf.Max(size.x, 1f));
        }

        private void HandleInertia()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || body == null || body.rb == null ||
                PauseHandler.main != null && PauseHandler.main.isPaused) return;
            Vector2 velocity = body.rb.velocity;
            if (inertiaBody != body || inertiaSlot != body.handSlot)
            {
                if (!ReferenceEquals(inertiaBody, null)) inertia.Reset();
                inertiaBody = body;
                inertiaSlot = body.handSlot;
                inertiaHand = null;
                if (body.slots != null && body.handSlot >= 0 && body.handSlot < body.slots.Length &&
                    body.slots[body.handSlot] != null && body.slots[body.handSlot].limb != null)
                    inertiaHand = body.slots[body.handSlot].limb.rb;
                lastBodyVelocity = velocity;
                lastHandVelocity = inertiaHand != null ? inertiaHand.velocity - velocity : Vector2.zero;
                lastBodyPosition = body.rb.position;
                lastBodyAngularVelocity = body.rb.angularVelocity;
            }
            Vector2 handVelocity = inertiaHand != null ? inertiaHand.velocity - velocity : Vector2.zero;
            if (dt > 0.25f || (body.rb.position - lastBodyPosition).sqrMagnitude > 400f)
            {
                inertia.Reset(); // Loading/teleportation is not a physical impact.
                ResetFannyPackZip();
            }
            else
            {
                // Velocity changes capture acceleration, braking, jumps, landings and external impacts.
                Vector2 impulse = Vector2.ClampMagnitude(velocity - lastBodyVelocity, 30f) * -0.025f +
                    Vector2.ClampMagnitude(handVelocity - lastHandVelocity, 15f) * -0.008f;
                impulse.y -= Mathf.Clamp(body.rb.angularVelocity - lastBodyAngularVelocity, -360f, 360f) * 0.0001f;
                inertia.ImpulseAtGrip(impulse * inertia.Mass);
                Vector2 movementChange = Vector2.ClampMagnitude(velocity - lastBodyVelocity, 30f);
                AddFannyPackZipImpulse(-movementChange.x * 10f + movementChange.y * 2f +
                    Mathf.Clamp(body.rb.angularVelocity - lastBodyAngularVelocity, -180f, 180f) * 0.05f);
            }
            lastBodyVelocity = velocity;
            lastHandVelocity = handVelocity;
            lastBodyPosition = body.rb.position;
            lastBodyAngularVelocity = body.rb.angularVelocity;

            Vector2 handTarget = new Vector2(-velocity.x * 0.0015f,
                -velocity.y * 0.001f - Mathf.Clamp01(body.crouchAmount) * 0.04f);
            handTarget += painTremor / Mathf.Max(size.x, 1f);
            handTarget.y += Mathf.Clamp(body.rb.angularVelocity * 0.0001f, -0.03f, 0.03f);
            float strength = Mathf.Clamp(0.65f + body.skills.STRFrom10 * 0.05f, 0.5f, 1.35f) *
                Mathf.Clamp(body.consciousness * 0.01f, 0.2f, 1f);
            if (!body.conscious) strength *= 0.35f;
            if (holdsSlide) strength *= 1.3f;
            UpdateSliderPull();
            inertia.Step(dt, handTarget, strength);
            Vector2 position = inertia.Position * size.x;
            gunBase.transform.localPosition = new Vector3(position.x, position.y, 0f);
            gunBase.transform.localRotation = Quaternion.Euler(0f, 0f, inertia.Angle * Mathf.Rad2Deg);
        }

        private void OnDisable() => ResetInertia();
    }
}
