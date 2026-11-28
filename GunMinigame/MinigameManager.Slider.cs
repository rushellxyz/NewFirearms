using UnityEngine;

namespace GunMinigame
{
    public partial class MinigameManager
    {
        internal Transform BoltSlider => info.independentFrontSlider
            ? sliderBackImage.transform : sliderFrontImage.transform;

        private void SetSliderPosition(Vector2 position, bool driven)
        {
            float boltX = GetDrivenBoltPosition(position.x, driven);
            sliderBackImage.transform.localPosition = new Vector3(boltX, info.mosynStyleRack ? 0f : position.y);
            if (info.independentFrontSlider && !driven)
            {
                Vector3 current = sliderFrontImage.transform.localPosition;
                position = new Vector2(
                    SliderAnimation.Move(current.x, info.unrackedIdlePosition, Time.deltaTime),
                    SliderAnimation.Move(current.y, 0f, Time.deltaTime));
            }
            sliderFrontImage.transform.localPosition = position;
            sliderLastXPos = boltX;
        }

        internal void SetSliderAngle(float angle, bool driven)
        {
            if (info.independentFrontSlider)
            {
                sliderBackImage.transform.localRotation = Quaternion.Euler(0f, 0f, GetDrivenBoltPosition(angle, driven));
                if (!driven)
                    angle = SliderAnimation.MoveAngle(sliderFrontImage.transform.localEulerAngles.z,
                        info.unrackedIdlePosition, Time.deltaTime);
            }
            sliderFrontImage.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private float GetDrivenBoltPosition(float position, bool driven)
        {
            // A pulled handle can move farther back, but it cannot push a cocked open bolt forward.
            if (!info.independentFrontSlider || !driven || !UsesOpenBolt || !gun.IsRacked())
                return position;
            return info.sliderMinimumPosition <= info.sliderMaximumPosition
                ? Mathf.Min(position, info.rackedIdlePosition)
                : Mathf.Max(position, info.rackedIdlePosition);
        }

        private void ResetIndependentSlider()
        {
            if (!info.independentFrontSlider) return;
            if (info.rackByRotation && !info.mosynStyleRack)
            {
                sliderBackImage.transform.localRotation = sliderFrontImage.transform.localRotation;
                sliderFrontImage.transform.localRotation = Quaternion.Euler(0f, 0f, info.unrackedIdlePosition);
            }
            else
                sliderFrontImage.transform.localPosition = new Vector3(info.unrackedIdlePosition, 0f);
        }
    }
}
