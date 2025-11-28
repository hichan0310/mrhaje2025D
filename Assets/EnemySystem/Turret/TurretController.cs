// Assets/EnemySystem/Turret/TurretController.cs
using UnityEngine;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Weapons;

namespace EnemySystem
{
    public enum TurretState
    {
        Idle,
        Firing,
        Dead
    }

    /// <summary>
    /// Stationary turret that fires dense bullet patterns straight forward.
    /// No animation; can be extended later with Animator triggers.
    /// </summary>
    public class TurretController : EnemyBase
    {

 
        [SerializeField] private float detectHalfHeight = 2f;
        [SerializeField] private LayerMask lineOfSightMask = ~0;
        [SerializeField] private bool requireLineOfSight = true;

        [Header("Firing")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0.8f, 0f);

        [SerializeField] private float waveInterval = 0.25f;  // fast fire rate
        [SerializeField] private int bulletsPerWave = 3;      // 3-way pattern
        [SerializeField] private float spreadAngle = 20f;     // narrow spread
        [SerializeField] private float projectilePower = 1f;
        [SerializeField] private float projectileSize = 0f;

        private TurretState state = TurretState.Idle;
        private float waveTimer;

        protected override void Start()
        {
            base.Start();

            // Turret should not move
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            // Disable automatic facing in inspector just in case
            faceTarget = false;

            if (EnemyStat != null)
            {

                EnemyStat.knockbackResist = 1.0f;
            }

            // Auto-acquire player if target is not set
            if (target == null)
            {
                var player = FindObjectOfType<PlayerSystem.Player>();
                if (player != null)
                    target = player.transform;
            }

            waveTimer = waveInterval;
        }

        /// <summary>
        /// Override facing logic so turret never rotates.
        /// </summary>
        protected override void UpdateFacing()
        {
            // Do nothing: keep original orientation
            return;
        }

        protected override void TickAI(float deltaTime)
        {
            if (state == TurretState.Dead) return;

            bool canFire = HasValidTargetInFront();

            if (!canFire)
            {
                state = TurretState.Idle;
                return;
            }

            state = TurretState.Firing;

            waveTimer -= deltaTime;
            if (waveTimer <= 0f)
            {
                FireWave();
                waveTimer = waveInterval;
            }
        }

        protected override void TickMovement(float fixedDeltaTime)
        {
            // Turret does not move
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        private bool HasValidTargetInFront()
        {
            if (target == null) return false;

            Vector2 selfPos = transform.position;
            Vector2 targetPos = target.position;
            Vector2 toTarget = targetPos - selfPos;

            float dist = toTarget.magnitude;
            if (dist > detectRange) return false;

            float dy = Mathf.Abs(toTarget.y);
            if (dy > detectHalfHeight) return false;

            // Forward direction based on initial localScale.x
            float facing = transform.localScale.x >= 0 ? 1f : -1f;

            // Target must be in front
            if (Mathf.Sign(toTarget.x) != Mathf.Sign(facing))
                return false;

            if (!requireLineOfSight)
                return true;

            // Simple line-of-sight check
            Vector2 origin = GetMuzzleWorldPosition(facing);
            Vector2 dir = toTarget.normalized;

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, lineOfSightMask);
            if (!hit) return false;

            Entity hitEntity = hit.collider.GetComponentInParent<Entity>();
            if (hitEntity == null) return false;

            Entity targetEntity = target.GetComponent<Entity>();
            if (targetEntity == null) return false;

            return hitEntity == targetEntity;
        }

        private Vector2 GetMuzzleWorldPosition(float facing)
        {
            return (Vector2)transform.position +
                   new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
        }

        private void FireWave()
        {
            if (projectilePrefab == null) return;

            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            Vector2 spawnOrigin = GetMuzzleWorldPosition(facing);

            // Purely horizontal forward direction
            Vector2 forward = new Vector2(facing, 0f);

            int count = Mathf.Max(1, bulletsPerWave);
            float totalSpread = spreadAngle;
            float startAngle = -totalSpread * 0.5f;
            float step = (count > 1) ? (totalSpread / (count - 1)) : 0f;

            for (int i = 0; i < count; i++)
            {
                float angleDeg = startAngle + step * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                Vector2 dir = new Vector2(
                    forward.x * Mathf.Cos(angleRad) - forward.y * Mathf.Sin(angleRad),
                    forward.x * Mathf.Sin(angleRad) + forward.y * Mathf.Cos(angleRad)
                );

                Projectile proj = Object.Instantiate(projectilePrefab, spawnOrigin, Quaternion.identity);
                proj.Initialize(this, dir, projectilePower, projectileSize);
            }

            // Later, you can add animation trigger here:
            // if (animator != null) animator.SetTrigger("Fire");
        }

        protected override void OnDie(Entity attacker)
        {
            if (state == TurretState.Dead) return;
            state = TurretState.Dead;

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
            // Optional: react to incoming events if needed
        }

        private void OnDrawGizmosSelected()
        {
            float facing = transform.localScale.x >= 0 ? 1f : -1f;

            // Detection box
            Gizmos.color = Color.red;
            Vector3 center = transform.position + new Vector3(facing * detectRange * 0.5f, 0f, 0f);
            Vector3 size = new Vector3(detectRange, detectHalfHeight * 2f, 0f);
            Gizmos.DrawWireCube(center, size);

            // Firing spread preview
            if (projectilePrefab == null) return;

            Gizmos.color = Color.yellow;
            Vector2 origin = GetMuzzleWorldPosition(facing);
            Vector2 forward = new Vector2(facing, 0f);

            int previewRays = 3;
            float previewSpread = spreadAngle;
            float startAngle = -previewSpread * 0.5f;
            float step = (previewRays > 1) ? (previewSpread / (previewRays - 1)) : 0f;

            for (int i = 0; i < previewRays; i++)
            {
                float angleDeg = startAngle + step * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                Vector2 dir = new Vector2(
                    forward.x * Mathf.Cos(angleRad) - forward.y * Mathf.Sin(angleRad),
                    forward.x * Mathf.Sin(angleRad) + forward.y * Mathf.Cos(angleRad)
                );

                Gizmos.DrawRay(origin, dir.normalized * 3f);
            }
        }
    }
}
