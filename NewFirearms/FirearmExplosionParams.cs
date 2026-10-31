using UnityEngine;

namespace NewFirearms
{
    public class FirearmExplosionParams : ExplosionParams
    {
        public float projectileVelocity = 40f;
        public float minimumTravelDistance;

        internal ExplosionParams AtPosition(Vector2 point)
        {
            var explosion = (FirearmExplosionParams)MemberwiseClone();
            explosion.position = point;
            return explosion;
        }
    }
}
