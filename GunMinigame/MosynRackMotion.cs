using UnityEngine;

namespace GunMinigame
{
    internal sealed class MosynRackMotion
    {
        // TODO Refactor
        internal float X { get; private set; }
        internal bool LockedDown { get; private set; }
        private Vector2 dragStart;
        private float dragStartX;

        internal void SetPose(float x, bool lockedDown)
        {
            X = x;
            LockedDown = lockedDown;
        }

        internal void BeginDrag(Vector2 pointer)
        {
            dragStart = pointer;
            dragStartX = X;
        }

        // Return the desired gun state only when the gesture crosses a transition.
        internal bool? Drag(Vector2 pointer, bool racked, float minimum, float maximum, float rackPoint, float lift)
        {
            Vector2 delta = pointer - dragStart;
            if (LockedDown)
            {
                if (X >= maximum && delta.y >= lift * 0.5f)
                {
                    LockedDown = false;
                    BeginDrag(pointer);
                }
                return null;
            }

            X = Mathf.Clamp(dragStartX + delta.x, minimum, maximum);
            if (X >= maximum && delta.y <= -lift * 0.5f)
            {
                LockedDown = true;
                BeginDrag(pointer);
                return racked ? false : (bool?)null;
            }
            return !racked && X < rackPoint ? true : (bool?)null;
        }
    }
}
