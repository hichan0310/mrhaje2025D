using EntitySystem.Events;
using EntitySystem.StatSystem;
using GameBackend;
using UnityEngine;
using static EntitySystem.StatSystem.EntityStat;

namespace EntitySystem
{
    /// <summary>
    /// Base class for all enemies.
    /// - Inherits Entity (hp, hpBar, event system)
    /// - Initializes EnemyStat if not assigned
    /// - Provides common targeting and facing logic
    /// </summary>
    public abstract class EnemyBase : Entity
    {
        [Header("Common Components")]
        [SerializeField] protected Rigidbody2D rb;

        [Header("Target & Detection")]
        [SerializeField] protected Transform target;
        [SerializeField] protected bool autoFindPlayer = true;
        [SerializeField] protected bool faceTarget = true;
        [SerializeField] protected float detectRange = 15f;

        [Header("Movement")]
        [SerializeField] protected float moveSpeed = 3f;

        [Header("Base Stat (for init)")]
        [SerializeField] protected int baseHp = 100;
        [SerializeField] protected int baseAtk = 10;
        [SerializeField] protected int baseDef = 0;
        [SerializeField] protected ArmorType armorType = ArmorType.Normal;
        [SerializeField] protected float baseKnockbackResist = 0.0f;

        protected bool isDead = false;

        /// <summary>
        /// Cast stat as EnemyStat. (null if not EnemyStat)
        /// </summary>
        protected EnemyStat EnemyStat
        {
            get { return stat as EnemyStat; }
        }

        protected override void Start()
        {
            base.Start(); // animator, TimeManager, hpBar

            if (!rb)
                rb = GetComponent<Rigidbody2D>();

            // Initialize stat as EnemyStat if not assigned
            if (stat == null)
            {
                stat = new EnemyStat(
                    this,
                    baseHp,
                    baseAtk,
                    baseDef,
                    armorType,
                    baseKnockbackResist,
                    1f
                );
            }
            else
            {
                if (!(stat is EnemyStat))
                {
                    Debug.LogWarning(
                        $"{name}: stat is not EnemyStat. ArmorType / knockbackResist may not be used correctly."
                    );
                }
            }

            // Auto target player if requested and target is not set
            if (autoFindPlayer && target == null)
            {
                var player = FindObjectOfType<PlayerSystem.Player>();
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        protected override void update(float deltaTime)
        {
            // base Entity update (listeners, hpBar)
            base.update(deltaTime);

            if (isDead) return;

            UpdateFacing();
            TickAI(deltaTime);
        }

        protected virtual void FixedUpdate()
        {
            if (isDead) return;
            TickMovement(Time.fixedDeltaTime);
        }

        public override void eventActive(EventArgs e)
        {
            // forward to base listeners
            base.eventActive(e);

            // handle die event
            EntityDieEvent die = e as EntityDieEvent;
            if (die != null && die.entity == this && !isDead)
            {
                isDead = true;
                OnDie(die.attacker);
            }

            OnEvent(e);
        }

        protected virtual void UpdateFacing()
        {
            if (!faceTarget || target == null) return;

            float dx = target.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                scale.x = dx > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        // --------------------------------------------------------------------
        // Target / detection helpers (for all enemies)
        // --------------------------------------------------------------------

        protected bool HasTarget
        {
            get { return target != null; }
        }

        protected float DistanceToTargetSqr()
        {
            if (target == null) return float.PositiveInfinity;
            Vector2 diff = (Vector2)target.position - (Vector2)transform.position;
            return diff.sqrMagnitude;
        }

        protected float DistanceToTarget()
        {
            if (target == null) return float.PositiveInfinity;
            return Vector2.Distance(transform.position, target.position);
        }

        protected bool IsTargetWithinRange(float range)
        {
            if (target == null) return false;
            float r2 = range * range;
            return DistanceToTargetSqr() <= r2;
        }

        // --------------------------------------------------------------------
        // Functions to be implemented per enemy type
        // --------------------------------------------------------------------
        protected abstract void TickAI(float deltaTime);
        protected abstract void TickMovement(float fixedDeltaTime);
        protected abstract void OnDie(Entity attacker);
        protected virtual void OnEvent(EventArgs e) { }
    }
}
