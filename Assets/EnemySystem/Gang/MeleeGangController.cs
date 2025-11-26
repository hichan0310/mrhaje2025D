using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using System.Collections;
using UnityEngine;
using static EntitySystem.StatSystem.EntityStat;

namespace EnemySystem
{
    public enum MeleeEnemyState
    {
        Idle,
        Chase,
        Attack,
        Recover,
        Dead
    }

    public class MeleeEnemyController : EnemyBase
    {
        [Header("Range")]
        [SerializeField] private float detectRange = 6f;
        [SerializeField] private float meleeRange = 1.5f;

        [Header("Attack")]
        [SerializeField] private int baseDamage = 40;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackWindup = 0.2f;

        [Header("Hit Area")]
        [SerializeField] private Vector2 hitOffset = new Vector2(1.0f, 0f);
        [SerializeField] private float hitRadius = 5.0f;
        [SerializeField] private LayerMask targetLayer;

        [Header("Misc")]
        [SerializeField] private float recoverDuration = 0.3f;

        private MeleeEnemyState state = MeleeEnemyState.Idle;
        private float attackTimer;
        private Coroutine attackRoutine;

        private float debugTimer;

        protected override void Start()
        {
            base.Start();

            if (EnemyStat != null)
            {
                EnemyStat.armorType = ArmorType.Normal;
                EnemyStat.knockbackResist = 0.3f;
            }

            if (target == null)
            {
                Debug.Log($"[MeleeEnemy] Start: target is null on {name}");
            }
            else
            {
                Debug.Log($"[MeleeEnemy] Start: target = {target.name} on {name}");
            }
        }

        protected virtual void Awake()
        {
            if (target == null)
            {
                Entity player = FindObjectOfType<PlayerSystem.Player>();
                if (player != null)
                {
                    target = player.transform;
                    Debug.Log($"[MeleeEnemy] Awake: found player {player.name} as target on {name}");
                }
                else
                {
                    Debug.LogWarning($"[MeleeEnemy] Awake: could not find PlayerSystem.Player in scene for {name}");
                }
            }
        }

        protected override void TickAI(float deltaTime)
        {
            if (state == MeleeEnemyState.Dead) return;

            // Simple debug every 0.5 sec
            debugTimer += deltaTime;
            if (debugTimer >= 0.5f)
            {
                string targetInfo = (target == null) ? "null" : target.name;
                // Debug.Log($"[MeleeEnemy] TickAI: state={state}, target={targetInfo}, attackTimer={attackTimer:F2}");
                debugTimer = 0f;
            }

            if (target == null)
            {
                return;
            }

            if (attackTimer > 0f)
                attackTimer -= deltaTime;

            if (state == MeleeEnemyState.Attack || state == MeleeEnemyState.Recover)
                return;

            float dist = Vector2.Distance(transform.position, target.position);

            // Extra debug for distance check
            // (this will also show if meleeRange is too small)
            // Debug.Log($"[MeleeEnemy] Distance to target: {dist:F2}, meleeRange={meleeRange}");

            if (dist <= meleeRange && attackTimer <= 0f)
            {
                // Debug.Log("[MeleeEnemy] Condition met for StartAttack");
                StartAttack();
                return;
            }

            if (dist <= detectRange)
            {
                if (state != MeleeEnemyState.Chase)
                    // Debug.Log("[MeleeEnemy] Switching state to Chase");
                state = MeleeEnemyState.Chase;
            }
            else
            {
                if (state != MeleeEnemyState.Idle)
                    // Debug.Log("[MeleeEnemy] Switching state to Idle");
                state = MeleeEnemyState.Idle;
            }
        }

        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null) return;
            if (state == MeleeEnemyState.Dead) return;

            if (state == MeleeEnemyState.Attack || state == MeleeEnemyState.Recover)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            if (state == MeleeEnemyState.Chase && target != null)
            {
                float dx = target.position.x - transform.position.x;
                float dir = Mathf.Sign(dx);
                rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        private void StartAttack()
        {
            if (attackRoutine != null)
            {
                // Debug.Log("[MeleeEnemy] StartAttack called but attackRoutine is already running");
                return;
            }

            // Debug.Log("[MeleeEnemy] StartAttack: starting attack routine");
            state = MeleeEnemyState.Attack;
            attackTimer = attackCooldown;
            attackRoutine = StartCoroutine(AttackRoutine_NoAnimation());
        }

        private IEnumerator AttackRoutine_NoAnimation()
        {
            if (attackWindup > 0f)
            {
                // Debug.Log($"[MeleeEnemy] AttackRoutine: windup {attackWindup}s");
                yield return new WaitForSeconds(attackWindup);
            }

            // Debug.Log("[MeleeEnemy] AttackRoutine: calling MeleeHit");
            MeleeHit();

            if (recoverDuration > 0f)
            {
                // Debug.Log($"[MeleeEnemy] AttackRoutine: recover {recoverDuration}s");
                state = MeleeEnemyState.Recover;
                yield return new WaitForSeconds(recoverDuration);
            }

            // Debug.Log("[MeleeEnemy] AttackRoutine: back to Idle");
            state = MeleeEnemyState.Idle;
            attackRoutine = null;
        }

        public void MeleeHit()
        {
            if (state == MeleeEnemyState.Dead)
            {
                // Debug.Log("[MeleeEnemy] MeleeHit called while Dead");
                return;
            }

            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            Vector2 center = (Vector2)transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);

#if UNITY_EDITOR
            Debug.DrawLine(center, center + Vector2.up * 0.1f, Color.red, 0.25f);
#endif

            // Debug.Log($"[MeleeEnemy] MeleeHit: center={center}, radius={hitRadius}, layerMask={targetLayer.value}");

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius, targetLayer);

            // Debug.Log($"[MeleeEnemy] MeleeHit: hits length = {hits.Length}");

            for (int i = 0; i < hits.Length; i++)
            {
                Entity t = hits[i].GetComponentInParent<Entity>();
                // Debug.Log($"[MeleeEnemy] MeleeHit: hit collider={hits[i].name}, entity={(t == null ? "null" : t.name)}");

                if (t == null || t == this) continue;

                var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);
                // Debug.Log($"[MeleeEnemy] MeleeHit: sending DamageGiveEvent to {t.name}");
                new DamageGiveEvent(baseDamage, center, this, t, tags, 1).trigger();
            }
        }

        protected override void OnDie(Entity attacker)
        {
            if (state == MeleeEnemyState.Dead) return;
            // Debug.Log($"[MeleeEnemy] OnDie: killed by {(attacker == null ? "null" : attacker.name)}");
            state = MeleeEnemyState.Dead;

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
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
            // Optional: handle incoming events
        }

        private void OnDrawGizmosSelected()
        {
            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            Vector2 center = (Vector2)transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, hitRadius);
        }
    }
}
