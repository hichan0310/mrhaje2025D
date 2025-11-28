using System.Collections;
using UnityEngine;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Weapons; // Projectile script
using static EntitySystem.StatSystem.EntityStat;

namespace EnemySystem
{
    public enum GuardState
    {
        Patrol,
        Shoot,
        Recover,
        Dead
    }

    /// <summary>
    /// Guard enemy: move forward, shoot, turn, repeat.
    /// Currently uses a single sprite (no animation).
    /// Animation hooks are commented out for future use.
    /// </summary>
    public class GuardController : EnemyBase
    {
        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 3f;
        [SerializeField] private float patrolDuration = 1.5f;

        [Header("Shooting")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float shootWindup = 0.2f;
        [SerializeField] private float shootRecover = 0.4f;
        [SerializeField] private float shootInterval = 0.0f;
        [SerializeField] private float projectilePower = 1f;

        [Header("Targeting")]
        [SerializeField] private bool aimAtPlayer = true;
        [SerializeField] private float maxAimRange = 10f;

        // ---------------------------------------------------------------------
        // Animation hooks (for future art update)
        // ---------------------------------------------------------------------
        /*
        [Header("Animation")]
        [SerializeField] private string moveBoolName = "IsMoving";
        [SerializeField] private string shootTriggerName = "Shoot";
        */

        private GuardState state = GuardState.Patrol;
        private float patrolTimer;
        private float shootCooldown;
        private Coroutine routine;

        private float baseScaleX = 1f;
        private int facing = 1;

        protected override void Start()
        {
            base.Start();

            baseScaleX = Mathf.Abs(transform.localScale.x);
            facing = transform.localScale.x >= 0 ? 1 : -1;
            patrolTimer = patrolDuration;

            if (EnemyStat != null)
            {
                   // light armor
                EnemyStat.knockbackResist = 0.2f;
            }
        }

protected override void TickAI(float deltaTime)
{
    if (state == GuardState.Dead) return;

    if (shootCooldown > 0f)
        shootCooldown -= deltaTime;

    if (state != GuardState.Patrol)
        return;

    patrolTimer -= deltaTime;

    // Only start shooting if we have a target in detection range
    bool canShoot = HasTarget && IsTargetWithinRange(detectRange);

    if (patrolTimer <= 0f && shootCooldown <= 0f && canShoot)
    {
        StartShoot();
    }
    else if (patrolTimer <= 0f && !canShoot)
    {
        // Player is too far: just reset patrol timer and keep walking
        patrolTimer = patrolDuration;
    }
}

        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null) return;
            if (state == GuardState.Dead) return;

            Vector2 vel = rb.linearVelocity;

            if (state == GuardState.Patrol)
            {
                vel.x = facing * patrolSpeed;
                // SetMovingAnimation(true);
            }
            else
            {
                vel.x = 0f;
                // SetMovingAnimation(false);
            }

            rb.linearVelocity = vel;
        }

        private void StartShoot()
        {
            if (routine != null) return;

            if (projectilePrefab == null || firePoint == null)
            {
                Debug.LogWarning($"[Guard] Missing projectilePrefab or firePoint on {name}");
                return;
            }

            state = GuardState.Shoot;
            routine = StartCoroutine(ShootRoutine_NoAnimation());
        }

        private IEnumerator ShootRoutine_NoAnimation()
        {
            if (shootWindup > 0f)
                yield return new WaitForSeconds(shootWindup);

            // Animation version (for future):
            /*
            if (animator != null && !string.IsNullOrEmpty(shootTriggerName))
                animator.SetTrigger(shootTriggerName);
            */

            FireProjectile();

            if (shootRecover > 0f)
            {
                state = GuardState.Recover;
                yield return new WaitForSeconds(shootRecover);
            }

            FlipFacing();
            patrolTimer = patrolDuration;
            shootCooldown = shootInterval;

            state = GuardState.Patrol;
            routine = null;
        }

        private void FireProjectile()
        {
            Vector2 dir;

            if (aimAtPlayer && target != null)
            {
                Vector2 toTarget = (Vector2)target.position - (Vector2)firePoint.position;
                if (toTarget.sqrMagnitude > 0.0001f && toTarget.magnitude <= maxAimRange)
                {
                    dir = toTarget.normalized;
                }
                else
                {
                    dir = new Vector2(facing, 0f);
                }
            }
            else
            {
                dir = new Vector2(facing, 0f);
            }

            Projectile proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            proj.Initialize(this, dir, projectilePower, 0f);
        }

        private void FlipFacing()
        {
            facing *= -1;
            Vector3 s = transform.localScale;
            s.x = baseScaleX * facing;
            transform.localScale = s;
        }

        /*
        private void SetMovingAnimation(bool moving)
        {
            if (animator == null) return;
            if (string.IsNullOrEmpty(moveBoolName)) return;
            animator.SetBool(moveBoolName, moving);
        }
        */

        protected override void OnDie(Entity attacker)
        {
            if (state == GuardState.Dead) return;
            state = GuardState.Dead;

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

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
            // Optional: handle events (DamageTakeEvent, etc.)
        }
    }
}
