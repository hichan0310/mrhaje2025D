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

        [Header("Animation")]
        [SerializeField] private string moveBoolName = "IsMoving";
        [SerializeField] private string attackTriggerName = "Attack";

        [SerializeField] private bool flipToTarget = true;
        private float baseScaleX = 5f;


        protected override void Start()
        {
            base.Start();

            if (EnemyStat != null)
            {
                EnemyStat.armorType = ArmorType.Normal;
                EnemyStat.knockbackResist = 0.3f;
            }

            baseScaleX = Mathf.Abs(transform.localScale.x);
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

        private void UpdateFacing()
{
    if (!flipToTarget) return;
    if (target == null) return;
    if (state == MeleeEnemyState.Dead) return;

    float dx = target.position.x - transform.position.x;
    if (Mathf.Abs(dx) < 0.01f) return;

    float sign = Mathf.Sign(dx);
    Vector3 s = transform.localScale;
    s.x = baseScaleX * sign;
    transform.localScale = s;
}


protected override void TickAI(float deltaTime)
{
    if (state == MeleeEnemyState.Dead) return;

    debugTimer += deltaTime;
    if (debugTimer >= 0.5f)
    {
        string targetInfo = (target == null) ? "null" : target.name;
        debugTimer = 0f;
    }

    if (target == null)
    {
        return;
    }

    if (attackTimer > 0f)
        attackTimer -= deltaTime;

    if (state == MeleeEnemyState.Attack || state == MeleeEnemyState.Recover)
    {
        UpdateFacing();      // even during attack/recover, you can keep or remove this
        return;
    }

    float dist = Vector2.Distance(transform.position, target.position);

    if (dist <= meleeRange && attackTimer <= 0f)
    {
        StartAttack();
        UpdateFacing();
        return;
    }

    if (dist <= detectRange)
    {
        state = MeleeEnemyState.Chase;
    }
    else
    {
        state = MeleeEnemyState.Idle;
    }

    // <- here: always face target, regardless of Idle/Chase
    UpdateFacing();
}


        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null) return;
            if (state == MeleeEnemyState.Dead) return;

            bool isMoving = false;

            if (state == MeleeEnemyState.Attack || state == MeleeEnemyState.Recover)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            else if (state == MeleeEnemyState.Chase && target != null)
            {
                float dx = target.position.x - transform.position.x;
                float dir = Mathf.Sign(dx);
                rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
                if (Mathf.Abs(rb.linearVelocity.x) > 0.01f)
                    isMoving = true;
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            if (animator != null && !string.IsNullOrEmpty(moveBoolName))
            {
                animator.SetBool(moveBoolName, isMoving);
            }
        }

private void StartAttack()
{
    if (attackRoutine != null)
    {
        return;
    }

    state = MeleeEnemyState.Attack;
    attackTimer = attackCooldown;
    attackRoutine = StartCoroutine(AttackRoutine_WithAnimation());
}

private IEnumerator AttackRoutine_WithAnimation()
{
    if (attackWindup > 0f)
        yield return new WaitForSeconds(attackWindup);

    // Trigger attack animation
    if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
    {
        animator.SetTrigger(attackTriggerName);
    }
    else
    {
        // Fallback: if no animator, call hit directly
        MeleeHit();
    }

    // Note:
    // MeleeHit() will be called from the attack animation
    // via Animation Event at the correct timing.

    if (recoverDuration > 0f)
    {
        state = MeleeEnemyState.Recover;
        yield return new WaitForSeconds(recoverDuration);
    }

    state = MeleeEnemyState.Idle;
    attackRoutine = null;
}


public void MeleeHit()
{
    if (state == MeleeEnemyState.Dead)
        return;

    float facing = transform.localScale.x >= 0 ? 1f : -1f;
    Vector2 center = (Vector2)transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);

#if UNITY_EDITOR
    Debug.DrawLine(center, center + Vector2.up * 0.1f, Color.red, 0.25f);
#endif

    Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius, targetLayer);

    for (int i = 0; i < hits.Length; i++)
    {
        Entity t = hits[i].GetComponentInParent<Entity>();
        if (t == null || t == this) continue;

        var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);
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
