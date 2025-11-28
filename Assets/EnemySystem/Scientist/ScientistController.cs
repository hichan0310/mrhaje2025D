// Assets/EnemySystem/Scientist/ScientistController.cs
using UnityEngine;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;

namespace EnemySystem
{
    public class ScientistController : EnemyBase
    {
        [Header("Movement (small patrol)")]
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private Transform leftPatrolPoint;
        [SerializeField] private Transform rightPatrolPoint;

        [Header("EMP Throw")]
        [SerializeField] private EmpGrenade empPrefab;
        [SerializeField] private Vector2 throwOffset = new Vector2(0.4f, 0.8f);
        [SerializeField] private float empCooldown = 5f;
        [SerializeField] private float empMinRange = 2f;
        [SerializeField] private float empMaxRange = 8f;
        [SerializeField] private float throwForce = 7f;
        [SerializeField] private LayerMask empLineOfSightMask = ~0;
        [SerializeField] private bool empRequireLineOfSight = true;

        [Header("Summon")]
        [SerializeField] private EnemyBase robotPrefab;
        [SerializeField] private EnemyBase dronePrefab;
        [SerializeField] private Transform[] summonPoints;
        [SerializeField] private float summonInterval = 10f;
        [SerializeField] private int maxSummoned = 3;

        [Header("Misc")]
        [SerializeField] private float summonCheckRadius = 0.5f;

        private float empTimer;
        private float summonTimer;
        private int patrolDir = 1;

        protected override void Start()
        {
            base.Start();

            if (!rb) rb = GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }

            // Scientist: light armor
            if (EnemyStat != null)
            {

                EnemyStat.knockbackResist = 0.1f;
            }

            // Auto-acquire player if not set
            if (target == null)
            {
                var player = FindObjectOfType<PlayerSystem.Player>();
                if (player != null)
                    target = player.transform;
            }

            empTimer = empCooldown * 0.5f;
            summonTimer = summonInterval * 0.5f;
        }

        protected override void TickAI(float deltaTime)
        {
            if (isDead) return;
            if (target == null) return;

            empTimer -= deltaTime;
            summonTimer -= deltaTime;

            bool canThrowEmp = empTimer <= 0f && IsTargetInEmpRangeAndLOS();
            if (canThrowEmp)
            {
                ThrowEmp();
                empTimer = empCooldown;
                return;
            }

            bool canSummon = summonTimer <= 0f;
            if (canSummon)
            {
                TrySummon();
                summonTimer = summonInterval;
            }
        }

        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null || isDead) return;

            // If no patrol points are set, stay almost fixed in place
            if (leftPatrolPoint == null && rightPatrolPoint == null)
            {
                rb.linearVelocity = new Vector2(0f, 0f);
                return;
            }

            float x = transform.position.x;
            float speed = patrolSpeed;
            Vector2 vel = rb.linearVelocity;
            vel.y = 0f;

            vel.x = patrolDir * speed;

            if (leftPatrolPoint != null && x <= leftPatrolPoint.position.x)
            {
                patrolDir = 1;
            }
            else if (rightPatrolPoint != null && x >= rightPatrolPoint.position.x)
            {
                patrolDir = -1;
            }

            rb.linearVelocity = vel;
        }

        private bool IsTargetInEmpRangeAndLOS()
        {
            if (target == null) return false;

            Vector2 selfPos = transform.position;
            Vector2 targetPos = target.position;
            Vector2 toTarget = targetPos - selfPos;

            float dist = toTarget.magnitude;
            if (dist < empMinRange || dist > empMaxRange)
                return false;

            // Optional line-of-sight check
            if (!empRequireLineOfSight)
                return true;

            Vector2 origin = GetThrowOrigin();
            Vector2 dir = (targetPos - origin).normalized;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, empLineOfSightMask);
            if (!hit) return false;

            Entity hitEntity = hit.collider.GetComponentInParent<Entity>();
            if (hitEntity == null) return false;

            Entity targetEntity = target.GetComponent<Entity>();
            if (targetEntity == null) return false;

            return hitEntity == targetEntity;
        }

        private Vector2 GetThrowOrigin()
        {
            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            return (Vector2)transform.position +
                   new Vector2(throwOffset.x * facing, throwOffset.y);
        }

        private void ThrowEmp()
        {
            if (empPrefab == null || target == null) return;

            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            Vector2 origin = GetThrowOrigin();
            Vector2 toTarget = (Vector2)target.position - origin;

            // Simple arced direction toward target
            Vector2 dir = toTarget.normalized + Vector2.up * 0.5f;
            dir.Normalize();

            EmpGrenade grenade = Object.Instantiate(empPrefab, origin, Quaternion.identity);
            grenade.owner = this;

            Rigidbody2D grb = grenade.GetComponent<Rigidbody2D>();
            if (grb != null)
            {
                grb.gravityScale = 1.2f;
                grb.linearVelocity = dir * throwForce;
            }

            // If you add animations later, you can trigger them here:
            // if (animator != null) animator.SetTrigger("Throw");
        }

        private void TrySummon()
        {
            if ((robotPrefab == null && dronePrefab == null) || summonPoints == null || summonPoints.Length == 0)
                return;

            int currentSummoned = CountExistingSummons();
            if (currentSummoned >= maxSummoned)
                return;

            Transform spawnPoint = summonPoints[Random.Range(0, summonPoints.Length)];
            Vector3 spawnPos = spawnPoint.position;

            // randomly choose robot or drone if both exist
            EnemyBase prefabToSpawn = null;

            if (robotPrefab != null && dronePrefab != null)
            {
                prefabToSpawn = (Random.value < 0.5f) ? robotPrefab : dronePrefab;
            }
            else if (robotPrefab != null)
            {
                prefabToSpawn = robotPrefab;
            }
            else
            {
                prefabToSpawn = dronePrefab;
            }

            if (prefabToSpawn == null) return;

            // avoid overlapping spawns tightly at the same position
            Collider2D[] hits = Physics2D.OverlapCircleAll(spawnPos, summonCheckRadius);
            foreach (var h in hits)
            {
                if (h.GetComponentInParent<EnemyBase>() != null)
                {
                    // already something here
                    return;
                }
            }

            EnemyBase spawned = Object.Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            // You can set some flag on spawned if needed (e.g. summoned by scientist)
            // Example: spawned.tag = "SummonedEnemy";
        }

        private int CountExistingSummons()
        {
            // For now, just count all EnemyBase in scene except this.
            // If you need finer control, add a dedicated "summonedByScientist" flag.
            EnemyBase[] enemies = FindObjectsOfType<EnemyBase>(false);
            int count = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == this) continue;
                count++;
            }
            return count;
        }

        protected override void OnDie(Entity attacker)
        {
            if (isDead) return;
            isDead = true;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            // If you add animations later:
            // if (animator != null) animator.SetTrigger("Die");

            Destroy(gameObject, 1.5f);
        }

        protected override void OnEvent(EventArgs e)
        {
            // You can react to DamageTakeEvent, etc. here if needed.
        }

        private void OnDrawGizmosSelected()
        {
            // EMP range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, empMinRange);
            Gizmos.DrawWireSphere(transform.position, empMaxRange);

            // Summon points
            if (summonPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var t in summonPoints)
                {
                    if (t == null) continue;
                    Gizmos.DrawWireSphere(t.position, summonCheckRadius);
                }
            }
        }
    }
}

