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

    /// <summary>
    /// Ground robot that patrols and performs a charging attack when the player is close.
    /// No animation; uses plain SpriteRenderer.
    /// </summary>
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
        [SerializeField] private LayerMask hitLayerMask = ~0; // filter what can be damaged

        private CombatRobotState state = CombatRobotState.Patrol;
        private float stateTimer = 0f;
        private int patrolDir = 1;
        private int chargeDir = 1;

        protected override void Start()
        {
            base.Start();

            if (!rb) rb = GetComponent<Rigidbody2D>();

            if (EnemyStat != null)
            {
                EnemyStat.knockbackResist = 0.8f;
            }
        }

        protected override void TickAI(float deltaTime)
        {
            if (state == CombatRobotState.Dead) return;

            // Handle timers for non-patrol states
            if (state == CombatRobotState.Windup ||
                state == CombatRobotState.Charge ||
                state == CombatRobotState.Recover)
            {
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                {
                    if (state == CombatRobotState.Windup)
                    {
                        StartCharge();
                    }
                    else if (state == CombatRobotState.Charge)
                    {
                        EnterRecover();
                    }
                    else if (state == CombatRobotState.Recover)
                    {
                        state = CombatRobotState.Patrol;
                    }
                }
                return;
            }

            // Patrol state logic for starting a charge
            if (state == CombatRobotState.Patrol && target != null)
            {
                float dist = Vector2.Distance(transform.position, target.position);
                if (dist <= aggroRange)
                {
                    StartWindup();
                }
            }
        }

        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null) return;
            if (state == CombatRobotState.Dead) return;

            Vector2 vel = rb.linearVelocity;

            switch (state)
            {
                case CombatRobotState.Patrol:
                    HandlePatrolMovement(ref vel);
                    break;

                case CombatRobotState.Windup:
                    // Slow down horizontally
                    vel.x = Mathf.Lerp(vel.x, 0f, 10f * fixedDeltaTime);
                    break;

                case CombatRobotState.Charge:
                    vel.x = chargeDir * chargeSpeed;
                    break;

                case CombatRobotState.Recover:
                    vel.x = Mathf.Lerp(vel.x, 0f, 5f * fixedDeltaTime);
                    break;

                case CombatRobotState.Dead:
                    vel = Vector2.zero;
                    break;
            }

            rb.linearVelocity = vel;
        }

private void HandlePatrolMovement(ref Vector2 vel)
{
    float speed = patrolSpeed;
    float x = transform.position.x;

    // If there are no patrol boundaries → idle standing
    if (leftBoundary == null && rightBoundary == null)
    {
        vel.x = 0f;
        return;
    }

    // If only left exists → bounce from left
    if (leftBoundary != null && rightBoundary == null)
    {
        vel.x = patrolDir * speed;
        if (x <= leftBoundary.position.x)
            patrolDir = 1;
        return;
    }

    // If only right exists → bounce to the right
    if (rightBoundary != null && leftBoundary == null)
    {
        vel.x = patrolDir * speed;
        if (x >= rightBoundary.position.x)
            patrolDir = -1;
        return;
    }

    // Otherwise (both exist)
    vel.x = patrolDir * speed;
    if (x <= leftBoundary.position.x)
        patrolDir = 1;
    else if (x >= rightBoundary.position.x)
        patrolDir = -1;
}


        private void StartWindup()
        {
            if (state != CombatRobotState.Patrol) return;

            state = CombatRobotState.Windup;
            stateTimer = windupTime;

            // Decide charge direction based on target position
            if (target != null)
            {
                float dx = target.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.01f)
                    chargeDir = dx > 0 ? 1 : -1;
                else
                    chargeDir = transform.localScale.x >= 0 ? 1 : -1;
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

            // Filter by layer
            if (((1 << collision.collider.gameObject.layer) & hitLayerMask) == 0)
                return;

            Entity hitEntity = collision.collider.GetComponentInParent<Entity>();
            if (hitEntity != null && hitEntity != this)
            {
                var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);
                new DamageGiveEvent(chargeDamage, transform.position, this, hitEntity, tags, 1).trigger();
            }

            // Stop charging after any valid collision
            EnterRecover();
        }

        protected override void OnDie(Entity attacker)
        {
            if (state == CombatRobotState.Dead) return;
            state = CombatRobotState.Dead;

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
            // Optional: react to events if needed
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }
    }
}
