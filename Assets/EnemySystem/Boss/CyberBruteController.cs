using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Weapons;
using System.Collections;
using UnityEngine;
using static EntitySystem.StatSystem.EntityStat;

namespace EnemySystem
{
    public enum CyberBruteState
    {
        Idle,
        Chase,
        MeleeAttack,
        JumpAttack,
        MissileVolley,
        Recover,
        Dead
    }

    public class CyberBruteController : EnemyBase
    {
        [Header("Ranges")]
        [SerializeField] private float meleeRange = 1.5f;
        [SerializeField] private float midRange = 6f;
        [SerializeField] private float chaseRange = 10f;

        [Header("Cooldowns")]
        [SerializeField] private float meleeCooldown = 2f;
        [SerializeField] private float jumpCooldown = 5f;
        [SerializeField] private float missileCooldown = 4f;

        [Header("Parabolic Jump")]
        [SerializeField] private float jumpDuration = 1.0f;
        [SerializeField] private float jumpHeight = 4f;
        [SerializeField] private GameObject landingAoEPrefab;

        [Header("Melee (No Animator)")]
        [SerializeField] private float meleeDuration = 0.6f;

        [Tooltip("보스 중심에서 근접 히트박스까지의 오프셋 (x는 좌우, y는 위아래)")]
        [SerializeField] private Vector2 meleeHitOffset = new Vector2(1.0f, 0f);

        [Tooltip("근접 공격 판정 반지름")]
        [SerializeField] private float meleeHitRadius = 1.2f;

        [Tooltip("맞을 레이어 (Player 등)")]
        [SerializeField] private LayerMask meleeHitMask;

        [Tooltip("근접 공격 데미지")]
        [SerializeField] private int meleeDamage = 40;

        [Header("Melee Visual Flash")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color meleeFlashColor = Color.red;
        [SerializeField] private float meleeFlashDuration = 0.15f;

        [Header("Missile Volley")]
        [SerializeField] private Projectile missilePrefab;
        [SerializeField] private Transform missileSpawnPoint;
        [SerializeField] private int minMissiles = 5;
        [SerializeField] private int maxMissiles = 10;
        [SerializeField] private float missileInterval = 0.15f;
        [SerializeField] private float missilePower = 1f;
        [SerializeField] private float missileSize = 0f;

        [Header("Misc")]
        [SerializeField] private float decisionInterval = 0.3f;
        [SerializeField] private float recoverDuration = 1.0f;
        [SerializeField] private float deathDestroyDelay = 2.0f;

        private CyberBruteState state = CyberBruteState.Idle;
        private float decisionTimer;
        private float meleeTimer;
        private float jumpTimer;
        private float missileTimer;
        private Coroutine currentAction;

        private Color originalColor = Color.white;

        protected override void Start()
        {
            base.Start();

            // 방어 / 접촉 데미지 설정
            if (EnemyStat != null)
            {
                EnemyStat.armorType = ArmorType.SpecialArmor;
                EnemyStat.knockbackResist = 0.9f;
                EnemyStat.contactDamageMultiplier = 2.0f;
            }

            // 스프라이트 렌더러 자동 할당
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        // ===== AI 로직 =====
        protected override void TickAI(float deltaTime)
        {
            if (state == CyberBruteState.Dead) return;
            if (target == null) return;

            if (meleeTimer > 0f) meleeTimer -= deltaTime;
            if (jumpTimer > 0f) jumpTimer -= deltaTime;
            if (missileTimer > 0f) missileTimer -= deltaTime;

            // 행동 코루틴 진행 중이면 의사결정 스킵
            if (currentAction != null) return;

            decisionTimer -= deltaTime;
            if (decisionTimer > 0f) return;
            decisionTimer = decisionInterval;

            float dist = Vector2.Distance(transform.position, target.position);

            // 근접 공격
            if (dist <= meleeRange && meleeTimer <= 0f)
            {
                StartMelee();
                return;
            }

            // 중거리: 점프 공격 or 미사일 난사
            if (dist <= midRange && missileTimer <= 0f)
            {
                if (jumpTimer <= 0f && Random.value < 0.5f)
                    StartJumpAttack();
                else
                    StartMissileVolley();
                return;
            }

            // 추격 / 대기
            if (dist <= chaseRange)
            {
                state = CyberBruteState.Chase;
            }
            else
            {
                state = CyberBruteState.Idle;
            }
        }

        // ===== 이동 처리 =====
        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null) return;
            if (state == CyberBruteState.Dead) return;

            // 공격/회복 중에는 수평 이동 정지
            if (state == CyberBruteState.MeleeAttack ||
                state == CyberBruteState.JumpAttack ||
                state == CyberBruteState.MissileVolley ||
                state == CyberBruteState.Recover)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            if (state == CyberBruteState.Chase && target != null)
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

        // ===== 액션 =====
        #region Actions

        // --- Melee ---
        private void StartMelee()
        {
            if (currentAction != null) return;

            meleeTimer = meleeCooldown;
            state = CyberBruteState.MeleeAttack;
            currentAction = StartCoroutine(MeleeRoutine());
        }

        private IEnumerator MeleeRoutine()
        {
            // 애니메이터 대신 스프라이트 붉게 + 판정
            float hitTime = meleeDuration * 0.5f;
            float flashTime = Mathf.Min(meleeFlashDuration, meleeDuration * 0.5f);

            // 준비 모션 시간
            float preTime = Mathf.Max(hitTime - flashTime * 0.5f, 0f);
            if (preTime > 0f)
                yield return new WaitForSeconds(preTime);

            // 붉게 변환 시작
            if (spriteRenderer != null)
                spriteRenderer.color = meleeFlashColor;

            // 임팩트 직전 약간 딜레이
            float preHitFlash = flashTime * 0.5f;
            if (preHitFlash > 0f)
                yield return new WaitForSeconds(preHitFlash);

            // 실제 히트 판정
            DoMeleeHit();

            // 붉은색 유지 후 원래 색으로 복구
            float postHitFlash = flashTime * 0.5f;
            if (postHitFlash > 0f)
                yield return new WaitForSeconds(postHitFlash);

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;

            // 남은 애니메이션 시간 소모
            float remaining = Mathf.Max(meleeDuration - preTime - flashTime, 0f);
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

        private void DoMeleeHit()
        {
            // 좌우 방향에 따라 오프셋 방향 반영
            float facing = Mathf.Sign(transform.localScale.x == 0 ? 1 : transform.localScale.x);
            Vector2 center = (Vector2)transform.position
                             + new Vector2(meleeHitOffset.x * facing, meleeHitOffset.y);

            // 디버그용 표시
            Debug.DrawLine(center, center + Vector2.up * 0.2f, Color.red, 0.5f);
            Debug.Log($"CyberBrute Melee HIT check at {center}");

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeHitRadius, meleeHitMask);
            foreach (var col in hits)
            {
                if (col == null) continue;

                var entity = col.GetComponentInParent<Entity>();
                if (entity != null && entity != this)
                {
                    // 너네 damage 시스템에 맞게 수정해서 사용
                    // 예시 1: 단순 데미지 메서드
                    // entity.TakeDamage(meleeDamage, this);

                    // 예시 2: Damage 이벤트 시스템
                    // var ev = new DamageGiveEvent(this, entity, meleeDamage, DamageType.Melee);
                    // EventBus.Raise(ev);

                    Debug.Log($"CyberBrute melee hit → {entity.name}");
                }
            }
        }

        // --- Jump Attack ---
        private void StartJumpAttack()
        {
            if (currentAction != null) return;

            jumpTimer = jumpCooldown;
            state = CyberBruteState.JumpAttack;
            currentAction = StartCoroutine(JumpRoutine());
        }

        private IEnumerator JumpRoutine()
        {
            if (rb == null || target == null)
            {
                currentAction = null;
                state = CyberBruteState.Idle;
                yield break;
            }

            Vector2 startPos = rb.position;
            Vector2 endPos = target.position;

            float t = 0f;
            float originalGravity = rb.gravityScale;
            rb.gravityScale = 0f; // 수동 포물선 이동

            while (t < jumpDuration)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / jumpDuration);

                // 포물선 곡선
                float hCurve = 4f * n * (1f - n);
                Vector2 pos = Vector2.Lerp(startPos, endPos, n);
                pos.y += hCurve * jumpHeight;

                rb.MovePosition(pos);
                yield return null;
            }

            rb.gravityScale = originalGravity;

            // 착지 AoE 생성
            if (landingAoEPrefab != null)
            {
                Vector2 spawnPos = rb.position;
                Collider2D col = GetComponent<Collider2D>();
                if (col != null)
                    spawnPos.y -= col.bounds.extents.y;

                var aoeObj = Object.Instantiate(landingAoEPrefab, spawnPos, Quaternion.identity);

                var aoe = aoeObj.GetComponent<AoEAttack>();
                if (aoe != null)
                {
                    // 보스 Entity를 owner로 설정
                    aoe.owner = this;
                }
            }

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

        // --- Missile Volley ---
        private void StartMissileVolley()
        {
            if (currentAction != null) return;

            missileTimer = missileCooldown;
            state = CyberBruteState.MissileVolley;
            currentAction = StartCoroutine(MissileRoutine());
        }

        private IEnumerator MissileRoutine()
        {
            if (missilePrefab == null || missileSpawnPoint == null)
            {
                currentAction = null;
                state = CyberBruteState.Idle;
                yield break;
            }

            int count = Random.Range(minMissiles, maxMissiles + 1);

            for (int i = 0; i < count; i++)
            {
                if (target == null) break;

                Projectile proj = Object.Instantiate(
                    missilePrefab,
                    missileSpawnPoint.position,
                    Quaternion.identity
                );

                Vector2 dir = (target.position - missileSpawnPoint.position);
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

                // EnemyBase는 Entity 상속이라 owner로 this 전달 가능
                proj.Initialize(this, dir, missilePower, missileSize);

                yield return new WaitForSeconds(missileInterval);
            }

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

        // --- Recover ---
        private IEnumerator RecoverRoutine()
        {
            yield return new WaitForSeconds(recoverDuration);
            currentAction = null;
            if (!isDead)
            {
                state = CyberBruteState.Idle;
            }
        }

        #endregion

        // ===== 죽음 처리 =====
        protected override void OnDie(Entity attacker)
        {
            if (state == CyberBruteState.Dead) return;

            state = CyberBruteState.Dead;

            if (currentAction != null)
            {
                StopCoroutine(currentAction);
                currentAction = null;
            }
            StopAllCoroutines();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            // 죽을 때는 색만 살짝 어둡게 바꿔도 됨 (선택사항)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.gray;
            }

            Destroy(gameObject, deathDestroyDelay);
        }

        protected override void OnEvent(EventArgs e)
        {
            // 필요하면 이벤트 처리 추가
        }

        // 에디터에서 근접 히트박스 확인용
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            float facing = 1f;
            if (Application.isPlaying)
            {
                facing = Mathf.Sign(transform.localScale.x == 0 ? 1 : transform.localScale.x);
            }
            Vector2 center = (Vector2)transform.position
                             + new Vector2(meleeHitOffset.x * facing, meleeHitOffset.y);
            Gizmos.DrawWireSphere(center, meleeHitRadius);
        }
#endif
    }
}

