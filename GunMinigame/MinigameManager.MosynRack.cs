using System.Collections;
using UnityEngine;

namespace GunMinigame
{
    public partial class MinigameManager
    {
        private readonly MosynRackMotion mosynMotion = new MosynRackMotion();
        private bool mosynInitialized;
        private bool mosynObservedRacked;
        private bool? mosynRequestedRacked;
        private float mosynRequestTime;
        private float MosynLift => Mathf.Max(0.5f, Screen.height * 0.0175f);

        private Vector2 MosynPointer => gunBase.transform.InverseTransformPoint(SliderPointer);

        private void EnsureMosynRack()
        {
            if (mosynInitialized) return;
            mosynInitialized = true;
            ResetMosynRack(immediate: true);
        }

        private void ResetMosynRack(bool immediate = false)
        {
            mosynObservedRacked = gun.IsRacked();
            mosynRequestedRacked = null;
            mosynMotion.SetPose(mosynObservedRacked ? info.sliderMinimumPosition : info.sliderMaximumPosition,
                !mosynObservedRacked);
            mosynMotion.BeginDrag(MosynPointer);
            if (immediate)
                SetMosynSliderPosition(new Vector2(mosynMotion.X, mosynMotion.LockedDown ? 0f : MosynLift));
            else
                ApplyMosynPose();
        }

        private void BeginMosynDrag()
        {
            EnsureMosynRack();
            mosynMotion.BeginDrag(MosynPointer);
        }

        private void HandleMosynRack()
        {
            EnsureMosynRack();
            if (gun is IMinigameJammedGun jammed && jammed.TryGetJam(out _, out _))
            {
                ResetMosynRack();
                return;
            }
            bool racked = gun.IsRacked();
            if (mosynRequestedRacked.HasValue)
            {
                if (racked == mosynRequestedRacked.Value)
                {
                    mosynObservedRacked = racked;
                    mosynRequestedRacked = null;
                    mosynMotion.BeginDrag(MosynPointer);
                }
                else if (Time.unscaledTime - mosynRequestTime >= 1.5f)
                    ResetMosynRack();
                else
                {
                    ApplyMosynPose();
                    return;
                }
            }
            else if (racked != mosynObservedRacked)
                ResetMosynRack();

            Vector2 previousPosition = sliderFrontImage.transform.localPosition;
            if (holdsSlide)
            {
                bool? target = mosynMotion.Drag(MosynPointer, racked, info.sliderMinimumPosition,
                    info.sliderMaximumPosition, info.unrackedToRacked, info.rackedToUnracked, MosynLift);
                if (target.HasValue) RequestMosynRack(target.Value);
            }
            ApplyMosynPose();
            if (holdsSlide)
                AddSliderImpulse((Vector2)sliderFrontImage.transform.localPosition - previousPosition);
        }

        private void RequestMosynRack(bool target)
        {
            if (mosynRequestedRacked.HasValue || gun.IsRacked() == target) return;
            mosynRequestedRacked = target;
            mosynRequestTime = Time.unscaledTime;
            gun.Rack();
            if (gun.IsRacked() == target)
            {
                mosynObservedRacked = target;
                mosynRequestedRacked = null;
            }
        }

        private void ApplyMosynPose()
        {
            Vector2 current = sliderFrontImage.transform.localPosition;
            float x = holdsSlide ? mosynMotion.X
                : SliderAnimation.Move(current.x, mosynMotion.X, Time.deltaTime);
            float y = SliderAnimation.Move(current.y, mosynMotion.LockedDown ? 0f : MosynLift, Time.deltaTime);
            SetMosynSliderPosition(new Vector2(x, y));
        }

        private void SetMosynSliderPosition(Vector2 position)
        {
            sliderFrontImage.transform.localPosition = position;
            sliderBackImage.transform.localPosition = new Vector2(position.x, 0f);
            sliderFrontImage.transform.localRotation = Quaternion.identity;
            sliderBackImage.transform.localRotation = Quaternion.identity;
            sliderLastXPos = position.x;
        }

        private IEnumerator AnimateMosynRack()
        {
            EnsureMosynRack();
            if (mosynRequestedRacked.HasValue) yield break;
            automatedHandAction = true;
            try
            {
                bool wasRacked = gun.IsRacked();
                Vector3 gripPoint = GetMarksmanRackGripPoint();
                Vector2 sliderPosition = sliderFrontImage.transform.TransformPoint(gripPoint);
                handPos = sliderPosition + Vector2.down * (Screen.height * 0.08f);
                handTransform.position = handPos;
                yield return MoveAutomatedHand(sliderPosition, 0.045f);
                handImage.sprite = MinigameBase.main.handSprites[2];

                // Lift before any horizontal travel, then slide to the requested end.
                yield return MoveMosynSlider(new Vector2(mosynMotion.X, MosynLift), gripPoint, 0.04f);
                mosynMotion.SetPose(mosynMotion.X, false);
                float targetX = wasRacked ? info.sliderMaximumPosition : info.sliderMinimumPosition;
                yield return MoveMosynSlider(new Vector2(targetX, MosynLift), gripPoint, 0.06f);
                mosynMotion.SetPose(targetX, false);
                if (wasRacked)
                {
                    yield return MoveMosynSlider(new Vector2(targetX, 0f), gripPoint, 0.04f);
                    mosynMotion.SetPose(targetX, true);
                }
                RequestMosynRack(!wasRacked);

                handImage.sprite = MinigameBase.main.handSprites[UnityEngine.Random.Range(0, 2)];
                yield return MoveAutomatedHand(handPos + Vector2.down * (Screen.height * 0.08f), 0.045f);
            }
            finally
            {
                automatedHandAction = false;
                mosynMotion.BeginDrag(MosynPointer);
            }
        }

        private IEnumerator MoveMosynSlider(Vector2 target, Vector3 gripPoint, float duration)
        {
            Vector2 start = sliderFrontImage.transform.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetMosynSliderPosition(Vector2.Lerp(start, target, SliderAnimation.Ease(elapsed / duration)));
                handPos = sliderFrontImage.transform.TransformPoint(gripPoint);
                handTransform.position = handPos;
                yield return null;
            }
        }
    }
}
