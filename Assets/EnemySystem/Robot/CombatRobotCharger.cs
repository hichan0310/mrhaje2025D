// Assets/EnemySystem/Robot/CombatRobotCharger.cs
using UnityEngine;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;

namespace EnemySystem
{
    public enum CombatRobotState
    {
        Patrol,
        Windup,
        Charge,
        Recover,
        Dead
    }

    public class CombatRobotCharger : EnemyBase
    {
        [Header("Detection")]
        [SerializeField] private float aggroRange = 6f;

        [Header("Patrol")]
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private Transform leftBoundary;
        [SerializeField] private Transform rightBoundary;

        [Header("Charge Settings")]
        [SerializeField] private float windupTime = 0.4f;
        [SerializeField] private float chargeSpeed = 10f;
        [SerializeField] private float chargeDuration = 0.8f;
        [SerializeField] private int chargeDamage = 50;
        [SerializeField] private float recoverTime = 0.5f;

        [Header("Hit Filter")]
        [SerializeField] private LayerMask hitLayerMask; // Player only

        private CombatRobotState state = CombatRobotState.Patrol;
        private float stateTimer;
        private int patrolDir = 1;
        private int chargeDir = 1;

        protected override void Start()
        {
            base.Start();

            if (!rb) rb = GetComponent<Rigidbody2D>();

            // 필수 물리 세팅
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (EnemyStat != null)
                EnemyStat.knockbackResist = 0.8f;
        }

        protected override void TickAI(float deltaTime)
        {
            if (state == CombatRobotState.Dead) return;

            if (state == CombatRobotState.Windup ||
                state == CombatRobotState.Charge ||
                state == CombatRobotState.Recover)
            {
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                {
                    if (state == CombatRobotState.Windup)
                        StartCharge();
                    else if (state == CombatRobotState.Charge)
                        EnterRecover();
                    else if (state == CombatRobotState.Recover)
                        state = CombatRobotState.Patrol;
                }
                return;
            }

            // Patrol → Windup
            if (state == CombatRobotState.Patrol && target != null)
            {
                float dist = Vector2.Distance(transform.position, target.position);
                if (dist <= aggroRange)
                    StartWindup();
            }
        }

        protected override void TickMovement(float fixedDeltaTime)
        {
            if (!rb || state == CombatRobotState.Dead) return;

            Vector2 vel = rb.linearVelocity;

            switch (state)
            {
                case CombatRobotState.Patrol:
                    HandlePatrolMovement(ref vel);
                    break;

                case CombatRobotState.Windup:
                    vel.x = Mathf.Lerp(vel.x, 0f, 10f * fixedDeltaTime);
                    break;

                case CombatRobotState.Charge:
                    vel.x = chargeDir * chargeSpeed;
                    break;

                case CombatRobotState.Recover:
                    vel.x = Mathf.Lerp(vel.x, 0f, 5f * fixedDeltaTime);
                    break;
            }

            rb.linearVelocity = vel;
        }

        private void HandlePatrolMovement(ref Vector2 vel)
        {
            float x = transform.position.x;

            if (!leftBoundary && !rightBoundary)
            {
                vel.x = 0f;
                return;
            }

            vel.x = patrolDir * patrolSpeed;

            if (leftBoundary && x <= leftBoundary.position.x)
                patrolDir = 1;
            else if (rightBoundary && x >= rightBoundary.position.x)
                patrolDir = -1;
        }

        private void StartWindup()
        {
            if (state != CombatRobotState.Patrol) return;

            state = CombatRobotState.Windup;
            stateTimer = windupTime;

            if (target)
            {
                float dx = target.position.x - transform.position.x;
                chargeDir = dx >= 0f ? 1 : -1;
            }
            else
            {
                chargeDir = transform.localScale.x >= 0 ? 1 : -1;
            }
        }

        private void StartCharge()
        {
            state = CombatRobotState.Charge;
            stateTimer = chargeDuration;
        }

        private void EnterRecover()
        {
            state = CombatRobotState.Recover;
            stateTimer = recoverTime;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (state != CombatRobotState.Charge) return;
            TryHit(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (state != CombatRobotState.Charge) return;
            TryHit(collision.collider);
        }


        protected override void OnDie(Entity attacker)
        {
            if (state == CombatRobotState.Dead) return;
            state = CombatRobotState.Dead;

            if (rb)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            foreach (var col in GetComponentsInChildren<Collider2D>())
                col.enabled = false;

            Destroy(gameObject, 1.5f);
        }

        private void TryHit(Collider2D col)
{
    if (((1 << col.gameObject.layer) & hitLayerMask) == 0)
        return;

    Entity hitEntity = col.GetComponentInParent<Entity>();
    if (hitEntity == null || hitEntity == this)
        return;

    var tags = new AtkTagSet()
        .Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);

    new DamageGiveEvent(
        chargeDamage,
        transform.position,
        this,
        hitEntity,
        tags,
        1
    ).trigger();

    EnterRecover();
}


        protected override void OnEvent(EventArgs e) { }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }
    }
}
