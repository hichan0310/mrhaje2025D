using System;
using System.Linq;
using EntitySystem;
using EntitySystem.StatSystem;
using PlayerSystem;
using PlayerSystem.Weapons;
using UnityEngine;

namespace EnemySystem
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : Entity
    {
        [SerializeField] private EnemyDefinition definition = null;
        [SerializeField] private Rigidbody2D body = null;
        [SerializeField] private Transform projectileSpawnPoint = null;
        [SerializeField] private float targetRefreshInterval = 0.5f;
        [SerializeField] private float engagementRange = 12f;
        [SerializeField] private Player explicitTarget = null;

        private EnemyDefinition.ActionSequenceEntry[] sequence = Array.Empty<EnemyDefinition.ActionSequenceEntry>();
        private EnemyActionAsset currentAction = null;
        private int currentActionIndex = -1;
        private float currentActionDuration = 0f;
        private float actionTimer = 0f;
        private bool pendingAdvance = false;
        private float targetRefreshTimer = 0f;
        private Transform currentTarget = null;
        private bool isDead = false;
        private float initialScaleX = 1f;
        private float patrolDirection = 1f;

        public EnemyDefinition Definition => definition;
        public Transform Target => currentTarget;
        public Vector2 TargetDirection
        {
            get
            {
                if (!currentTarget)
                {
                    return Vector2.right * Mathf.Sign(transform.localScale.x);
                }

                return (currentTarget.position - transform.position).normalized;
            }
        }

        public Transform ProjectileOrigin => projectileSpawnPoint ? projectileSpawnPoint : transform;
        public float MoveSpeed => definition ? definition.MoveSpeed : 0f;
        public float PatrolDirection => patrolDirection;

        protected override void Start()
        {
            base.Start();

            if (!body)
            {
                body = GetComponent<Rigidbody2D>();
            }

            initialScaleX = Mathf.Abs(transform.localScale.x) > 0.001f ? Mathf.Abs(transform.localScale.x) : 1f;

            if (definition)
            {
                stat = new EntityStat(definition.BaseHealth, definition.BaseAttack, definition.BaseDefense)
                {
                    entity = this,
                    speed = definition.MoveSpeed
                };
                sequence = definition.ActionSequence == null
                    ? Array.Empty<EnemyDefinition.ActionSequenceEntry>()
                    : definition.ActionSequence.ToArray();
            }
            else
            {
                stat = new EntityStat(50, 10, 0) { entity = this };
                sequence = Array.Empty<EnemyDefinition.ActionSequenceEntry>();
            }

            AdvanceAction();
        }

        protected override void update(float deltaTime)
        {
            if (isDead)
            {
                return;
            }

            UpdateTarget(deltaTime);

            if (sequence.Length > 0 && currentAction != null)
            {
                currentAction.Tick(this, deltaTime);
                actionTimer += deltaTime;

                bool shouldAdvance = pendingAdvance;
                if (!shouldAdvance && currentActionDuration > 0f && actionTimer >= currentActionDuration)
                {
                    shouldAdvance = true;
                }

                pendingAdvance = false;
                if (shouldAdvance)
                {
                    AdvanceAction();
                }
            }

            base.update(deltaTime);

            if (stat != null && stat.nowHp <= 0)
            {
                HandleDeath();
            }
        }

        public void RequestNextAction()
        {
            pendingAdvance = true;
        }

        public void Move(Vector2 velocity, float deltaTime)
        {
            if (body)
            {
                body.linearVelocity = new Vector2(velocity.x, body.linearVelocity.y);
                body.MovePosition(body.position + velocity * deltaTime);
            }
            else
            {
                transform.position += (Vector3)(velocity * deltaTime);
            }

            if (Mathf.Abs(velocity.x) > 0.01f)
            {
                FaceDirection(Mathf.Sign(velocity.x));
            }
        }

        public void StopMovement()
        {
            if (body)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
        }

        public void InvertPatrolDirection()
        {
            patrolDirection = -patrolDirection;
        }

        public void FaceDirection(float directionSign)
        {
            if (Mathf.Approximately(directionSign, 0f))
            {
                return;
            }

            float sign = Mathf.Sign(directionSign);
            Vector3 scale = transform.localScale;
            scale.x = sign * initialScaleX;
            transform.localScale = scale;
        }

        public void FireProjectile(Projectile projectilePrefab, float power)
        {
            if (!projectilePrefab)
            {
                return;
            }

            Vector3 spawnPosition = ProjectileOrigin.position;
            Vector2 direction = TargetDirection;
            var instance = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            instance.Initialize(this, direction, power, 0f);
        }

        private void AdvanceAction()
        {
            currentAction?.OnExit(this);

            if (sequence.Length == 0)
            {
                currentAction = null;
                return;
            }

            actionTimer = 0f;
            currentActionIndex = (currentActionIndex + 1) % sequence.Length;
            var entry = sequence[currentActionIndex];
            currentAction = entry.action;
            currentActionDuration = Mathf.Max(0f, entry.duration);
            currentAction?.OnEnter(this);
        }

        private void UpdateTarget(float deltaTime)
        {
            targetRefreshTimer -= deltaTime;
            if (targetRefreshTimer > 0f && currentTarget)
            {
                return;
            }

            targetRefreshTimer = Mathf.Max(0.1f, targetRefreshInterval);

            if (explicitTarget)
            {
                currentTarget = explicitTarget.transform;
                return;
            }

            Player[] players = FindObjectsOfType<Player>();
            Transform best = null;
            float bestDistance = float.MaxValue;
            float maxRangeSqr = engagementRange <= 0f ? float.MaxValue : engagementRange * engagementRange;

            foreach (var player in players)
            {
                float distSqr = (player.transform.position - transform.position).sqrMagnitude;
                if (distSqr < bestDistance && distSqr <= maxRangeSqr)
                {
                    bestDistance = distSqr;
                    best = player.transform;
                }
            }

            currentTarget = best;
        }

        private void HandleDeath()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            currentAction?.OnExit(this);
            Destroy(gameObject);
        }
    }
}
