using UnityEngine;

namespace NewFirearms
{
    // Интересно, почему чат гпт хотел переписать гравитацию с нуля, пока я не указал ему на существование Rigidbody 

    // Only host projectiles resolve collisions. Replicas are visual and never explode locally.
    internal sealed class ExplosiveProjectile : MonoBehaviour
    {
        internal int NetworkId;
        private Rigidbody2D rb;
        internal Vector2 Position => rb.position;
        internal Vector2 Velocity => rb.velocity;
        private Vector2 previousPosition;
        private float distance;
        private AmmoInfo ammo;
        private Body shooter;
        private bool authoritative;
        private float age;
        private LineRenderer trail;

        internal static void Launch(AmmoInfo ammo, Vector2 position, Vector2 direction, Body shooter)
        {
            if (Plugin.togetherMpEnabled && !Plugin.IsHost()) return;
            var projectile = Create(position, direction.normalized * ammo.explosionParams.projectileVelocity);
            projectile.ammo = ammo;
            projectile.shooter = shooter;
            projectile.authoritative = true;
            if (Plugin.togetherMpEnabled)
            {
                ExplosiveProjectileNetwork.Spawn(projectile);
                float interval = Plugin.sett.projectileSyncRate;
                projectile.InvokeRepeating(nameof(Sync), interval, interval);
            }
        }

        internal static ExplosiveProjectile Create(Vector2 position, Vector2 velocity)
        {
            var projectile = new GameObject("NewFirearms_ExplosiveProjectile").AddComponent<ExplosiveProjectile>();
            projectile.transform.position = position;
            projectile.rb = projectile.gameObject.AddComponent<Rigidbody2D>();
            projectile.rb.bodyType = RigidbodyType2D.Dynamic;
            projectile.rb.gravityScale = 1f;
            projectile.rb.drag = 0f;
            projectile.rb.freezeRotation = true;
            projectile.rb.position = position;
            projectile.rb.velocity = velocity;
            projectile.previousPosition = position;
            if (!Plugin.togetherMpEnabled || !Plugin.IsDedicated())
            {
                var visual = (GameObject)Instantiate(Resources.Load("Special/TurretLine"), position, Quaternion.identity);
                visual.transform.SetParent(projectile.transform);
                projectile.trail = visual.GetComponent<LineRenderer>();
                projectile.trail.useWorldSpace = true;
                projectile.trail.startWidth = projectile.trail.endWidth = 0.12f;
                projectile.UpdateVisual();
            }
            return projectile;
        }

        internal void Correct(Vector2 position, Vector2 velocity)
        {
            rb.position = position;
            rb.velocity = velocity;
            previousPosition = position;
            age = 0f;
            UpdateVisual();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            age += dt;
            if (age > 30f)
            {
                Finish();
                return;
            }
            // Rigidbody2D advances movement and gravity during the physics step.
            // Sweep the actual travelled segment so fast projectiles still hit thin walls.
            Vector2 next = rb.position;
            if (authoritative && TryHit(next, out var hit))
            {
                bool armed = distance + Vector2.Distance(previousPosition, hit.point) >=
                    ammo.explosionParams.minimumTravelDistance;
                rb.position = hit.point;
                // Finish first, so damage callbacks cannot process this impact twice.
                Finish();
                if (armed)
                {
                    if (hit.collider.TryGetComponent<FirearmCourseTarget>(out var target))
                        target.Hit(shooter, Mathf.Max(ammo.structureDamage, ammo.animalDamage));
                    WorldGeneration.CreateExplosion(ammo.explosionParams.AtPosition(hit.point));
                }
                return;
            }
            distance += Vector2.Distance(previousPosition, next);
            previousPosition = next;
            UpdateVisual();
        }

        private void Sync()
        {
            if (enabled && authoritative && Plugin.togetherMpEnabled)
                ExplosiveProjectileNetwork.Sync(this, alive: true);
        }

        private bool TryHit(Vector2 next, out RaycastHit2D hit)
        {
            Vector2 delta = next - previousPosition;
            bool previous = Physics2D.queriesHitTriggers;
            RaycastHit2D[] hits;
            try
            {
                Physics2D.queriesHitTriggers = true;
                hits = Physics2D.RaycastAll(previousPosition, delta.normalized, delta.magnitude,
                    LayerMask.GetMask("Ground", "Body", "Limb", "Descriptor"));
            }
            finally { Physics2D.queriesHitTriggers = previous; }
            foreach (var candidate in hits)
            {
                var collider = candidate.collider;
                if (collider.gameObject.name.StartsWith("StunHitbox")) continue;
                if (shooter != null && (collider.GetComponentInParent<Body>() == shooter ||
                    collider.TryGetComponent<Limb>(out var limb) && limb.body == shooter)) continue;
                if (collider.TryGetComponent<BuildingEntity>(out var building) && building.cantHit) continue;
                if (collider.gameObject.layer != 6 && candidate.rigidbody == null &&
                    building == null && !collider.TryGetComponent<FirearmCourseTarget>(out _)) continue;
                hit = candidate;
                return true;
            }
            hit = default;
            return false;
        }

        private void UpdateVisual()
        {
            if (trail == null) return;
            trail.SetPosition(0, Position - Velocity.normalized * 0.3f);
            trail.SetPosition(1, Position);
        }

        private void Finish()
        {
            CancelInvoke(nameof(Sync));
            enabled = false;
            rb.simulated = false;
            if (authoritative && Plugin.togetherMpEnabled)
                ExplosiveProjectileNetwork.Sync(this, alive: false);
            Destroy(gameObject);
        }

        private void OnDestroy() => ExplosiveProjectileNetwork.Forget(NetworkId, this);
    }
}
