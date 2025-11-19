using EntitySystem.Events;
using EntitySystem.StatSystem;
using GameBackend;
using UnityEngine;
using static EntitySystem.StatSystem.EntityStat;

namespace EntitySystem
{
    /// <summary>
    /// 모든 적의 공통 베이스.
    /// - Entity 상속 → HP, hpBar, 이벤트 시스템 그대로 사용
    /// - EnemyStat을 기본 스탯으로 사용 (없으면 여기서 생성)
    /// </summary>
    public abstract class EnemyBase : Entity
    {
        [Header("Common Components")]
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected Transform target;
        [SerializeField] protected bool faceTarget = true;
        [SerializeField] protected float moveSpeed = 3f;

        [Header("Base Stat (for init)")]
        [SerializeField] protected int baseHp = 100;
        [SerializeField] protected int baseAtk = 10;
        [SerializeField] protected int baseDef = 0;
        [SerializeField] protected ArmorType armorType = ArmorType.Normal;
        [SerializeField] protected float baseKnockbackResist = 0.0f;

        protected bool isDead = false;

        /// <summary>
        /// EnemyStat로 캐스팅한 뷰. (아니면 null)
        /// </summary>
        protected EnemyStat EnemyStat
        {
            get { return stat as EnemyStat; }
        }

        protected override void Start()
        {
            base.Start(); // animator, TimeManager, hpBar 설정

            if (!rb) rb = GetComponent<Rigidbody2D>();

            // stat이 비어 있으면 EnemyStat으로 초기화
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
                // 만약 이미 Inspector에서 EnemyStat을 넣어놨다면 그대로 사용.
                // (EntityStat만 있다면 추가 필드는 못 쓰지만, 에러는 안 나게 두기)
                if (!(stat is EnemyStat))
                {
                    Debug.LogWarning(
                        $"{name}: stat이 EnemyStat이 아니라서 armorType/knockbackResist 같은 Enemy 전용 속성은 못 씀."
                    );
                }
            }
        }

        protected override void update(float deltaTime)
        {
            // 기존 리스너 업데이트 + hpBar 갱신
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
            // 먼저 Entity가 리스너들에게 뿌리게 함
            base.eventActive(e);

            // 내가 죽은 EntityDieEvent라면 OnDie 호출
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

        // 개별 적이 구현해야 하는 것들
        protected abstract void TickAI(float deltaTime);
        protected abstract void TickMovement(float fixedDeltaTime);
        protected abstract void OnDie(Entity attacker);
        protected virtual void OnEvent(EventArgs e) { }
    }
}
