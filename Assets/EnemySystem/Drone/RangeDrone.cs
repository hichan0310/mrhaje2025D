// Assets/EnemySystem/Drone/RangedDroneController.cs
using UnityEngine;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Weapons;

namespace EnemySystem
{
    /// <summary>
    /// Ranged flying enemy (drone).
    /// Keeps a comfortable distance from the target and shoots projectiles.
    /// Moves in a slightly wobbling pattern so it does not look too robotic.
    /// </summary>
    public class RangedDroneController : DroneBase
    {
        [Header("Patrol")]
        [SerializeField] private float horizontalPatrolSpeed = 2f;

        [Header("Attack")]
        [SerializeField] private float shootRange = 7f;
        [SerializeField] private float shootCooldown = 1.5f;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Vector2 shootOffset = new Vector2(0.6f, 0.2f);
        [SerializeField] private float projectilePower = 1f;

        [Header("Positioning")]
        [SerializeField] private float preferredDistance = 5f;   // desired distance from target
        [SerializeField] private float distanceTolerance = 1f;   // band around preferred distance
        [SerializeField] private float velocitySmooth = 6f;      // smoothing factor for velocity lerp

        [Header("Wobble")]
        [SerializeField] private float wobbleAmplitude = 0.3f;   // sideways wobble amount
        [SerializeField] private float wobbleFrequency = 1.5f;   // wobble speed

        private float cooldownTimer;

        protected override void TickAI(float deltaTime)
        {
            if (isDead) return;

            if (cooldownTimer > 0f)
                cooldownTimer -= deltaTime;

            if (!HasTarget) return;

            float dist = DistanceToTarget();

            // Shoot only if inside shoot range
            if (dist <= shootRange && cooldownTimer <= 0f)
            {
                Fire();
                cooldownTimer = shootCooldown;
            }
        }

        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null || isDead) return;

            Vector2 currentVel = rb.linearVelocity;
            Vector2 desiredVel = Vector2.zero;

            // No target → simple horizontal patrol
            if (!HasTarget)
            {
                float dir = Mathf.Sign(transform.localScale.x);
                desiredVel = new Vector2(dir * horizontalPatrolSpeed, 0f);
                rb.linearVelocity = Vector2.Lerp(currentVel, desiredVel, velocitySmooth * fixedDeltaTime);
                return;
            }

            // With target: softly maintain preferredDistance and adjust Y as well
            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            float dist = toTarget.magnitude;

            float minDist = preferredDistance - distanceTolerance;
            float maxDist = preferredDistance + distanceTolerance;

            Vector2 baseDir = Vector2.zero;

            if (dist > maxDist)
            {
                // Too far → move closer
                baseDir = toTarget.normalized;
            }
            else if (dist < minDist)
            {
                // Too close → move away
                baseDir = -toTarget.normalized;
            }
            else
            {
                // Within comfortable band → no strong radial movement
                baseDir = Vector2.zero;
            }

            // Perpendicular direction for sideways wobble
            Vector2 perp = Vector2.zero;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector2 dirToTarget = toTarget.normalized;
                perp = new Vector2(-dirToTarget.y, dirToTarget.x); // 90 degrees
            }

            float wobble = Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmplitude;

            if (baseDir != Vector2.zero)
            {
                // Move toward/away from target with a slight sideways wobble
                Vector2 moveDir = (baseDir + perp * wobble).normalized;
                desiredVel = moveDir * moveSpeed;
            }
            else
            {
                // Stay around current distance but wobble sideways
                desiredVel = perp * wobble * moveSpeed;
            }

            // Smoothly move current velocity toward desired velocity
            rb.linearVelocity = Vector2.Lerp(currentVel, desiredVel, velocitySmooth * fixedDeltaTime);
        }

        private void Fire()
        {
            if (projectilePrefab == null) return;

            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            Vector2 spawnPos = (Vector2)transform.position +
                               new Vector2(shootOffset.x * facing, shootOffset.y);

            Vector2 dir;

            if (HasTarget)
            {
                Vector2 toTarget = (Vector2)target.position - spawnPos;
                if (toTarget.sqrMagnitude > 0.0001f)
                    dir = toTarget.normalized;
                else
                    dir = new Vector2(facing, 0f);
            }
            else
            {
                dir = new Vector2(facing, 0f);
            }

            Projectile proj = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            proj.Initialize(this, dir, projectilePower, 0f);

            // If you add animations later, you can trigger them here:
            // if (animator != null) animator.SetTrigger("Shoot");
        }

        protected override void OnDie(Entity attacker)
        {
            // EnemyBase has already set isDead = true before calling this

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Destroy(gameObject, 1.5f);
        }

        protected override void OnEvent(EventArgs e)
        {
            // Optional: react to events (DamageTakeEvent, etc.)
        }
    }
}
