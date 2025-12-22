using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using EnemySystem.Projectiles;
using PlayerSystem;
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
        GrabAttack,
        MissileVolley,
        JumpAttack,
        Recover,
        Dead
    }

    public class CyberBruteController : EnemyBase
    {
        /* ===================== RANGE ===================== */
        [Header("Ranges")]
        [SerializeField] private float meleeRange = 1.5f;
        [SerializeField] private float midRange = 6f;
        [SerializeField] private float chaseRange = 12f;

        /* ===================== COOLDOWN ===================== */
        [Header("Cooldowns")]
        [SerializeField] private float meleeCooldown = 2f;
        [SerializeField] private float grabCooldown = 6f;
        [SerializeField] private float missileCooldown = 4f;
        [SerializeField] private float jumpCooldown = 5f;

        /* ===================== MELEE ===================== */
        [Header("Melee")]
        [SerializeField] private int meleeDamage = 40;
        [SerializeField] private float meleeWindup = 0.2f;
        [SerializeField] private float meleeRecover = 1.0f;

        /* ===================== GRAB ===================== */
        [Header("Grab")]
        [SerializeField] private GrabProjectile grabProjectilePrefab;
        [SerializeField] private Transform grabSpawnPoint;
        [SerializeField, Range(0f, 1f)] private float grabChance = 0.3f; // 30%
        [SerializeField] private float grabPullDuration = 0.6f;
        [SerializeField] private float grabPullOffsetX = 1.2f;
        [SerializeField] private float grabMissTimeout = 0.8f; // miss timeout

        [Header("Grab Visual")]
        [SerializeField] private GameObject grabTextPrefab;

        /* ===================== MISSILE ===================== */
        [Header("Missile")]
        [SerializeField] private Projectile missilePrefab;
        [SerializeField] private Transform missileSpawnPoint;
        [SerializeField] private int minMissiles = 5;
        [SerializeField] private int maxMissiles = 10;
        [SerializeField] private float missileInterval = 0.15f;
        [SerializeField] private float missilePower = 1f;
        [SerializeField] private float missileSize = 0f;

        /* ===================== JUMP (LONG RANGE) ===================== */
        [Header("Jump Attack")]
        [SerializeField] private float jumpUpSpeed = 18f;
        [SerializeField] private float hoverTime = 0.4f;
        [SerializeField] private float slamDownSpeed = 30f;
        [SerializeField] private GameObject landingAoEPrefab;

        [Header("Ground Check (for Jump Landing)")]
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private float groundCheckDistance = 0.25f;

        private bool jumpWaitingAnim;   // 애니메이션 끝 대기
        private bool jumpInProgress;    // 실제 점프 진행 중
        private Animator animator;

        /* ===================== MISC ===================== */
        [Header("Misc")]
        [SerializeField] private float decisionInterval = 0.3f;
        [SerializeField] private float recoverDuration = 1.0f;
        [SerializeField] private float deathDestroyDelay = 2.0f;

        /* ===================== INTERNAL ===================== */
        private CyberBruteState state = CyberBruteState.Idle;

        private float decisionTimer;
        private float meleeTimer;
        private float grabTimer;
        private float missileTimer;
        private float jumpTimer;

        private Coroutine currentAction;

        protected override void Start()
        {
            base.Start();
            animator = GetComponentInChildren<Animator>();
            // 방어 / 접촉 데미지 설정
            if (EnemyStat != null)
            {
                EnemyStat.armorType = ArmorType.SpecialArmor;
                EnemyStat.knockbackResist = 0.9f;
                EnemyStat.contactDamageMultiplier = 2.0f;
            }
        }

        /* ===================== AI ===================== */
        protected override void TickAI(float deltaTime)
        {
            if (state == CyberBruteState.Dead || target == null) return;
            if (jumpWaitingAnim || jumpInProgress)
                return;
            meleeTimer -= deltaTime;
            grabTimer -= deltaTime;
            missileTimer -= deltaTime;
            jumpTimer -= deltaTime;

            // 행동 코루틴 진행 중이면 의사결정 스킵
            if (currentAction != null) return;

            decisionTimer -= deltaTime;
            if (decisionTimer > 0f) return;
            decisionTimer = decisionInterval;

            float dist = Vector2.Distance(transform.position, target.position);

            // 근접
            if (dist <= meleeRange && meleeTimer <= 0f)
            {
                StartMelee();
                return;
            }

            // 중거리 (그랩 30 / 미사일 70)
            if (dist <= midRange)
            {
                if (grabTimer <= 0f && Random.value < grabChance)
                {
                    StartGrab();
                    return;
                }

                if (missileTimer <= 0f)
                {
                    StartMissileVolley();
                    return;
                }
            }

            // 장거리 (점프)
            if (dist <= chaseRange && jumpTimer <= 0f)
            {
                StartJumpAttack();
                return;
            }

            // 기본 추격
            state = CyberBruteState.Chase;
        }

        /* ===================== MOVEMENT ===================== */
        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null || state == CyberBruteState.Dead) return;

            // 공격/회복 중에는 수평 이동 정지
            if (state != CyberBruteState.Chase)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            if (target == null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            float dir = Mathf.Sign(target.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        }

        /* ===================== ACTIONS ===================== */

        /* ---------- MELEE ---------- */
        private void StartMelee()
        {
            meleeTimer = meleeCooldown;
            state = CyberBruteState.MeleeAttack;
            currentAction = StartCoroutine(MeleeRoutine());
        }

        private IEnumerator MeleeRoutine()
        {
            // windup
            if (meleeWindup > 0f)
                yield return new WaitForSeconds(meleeWindup);

            // "근접이면" 때린다고 가정하고 이벤트 발생 (거리 체크 추가하고 싶으면 여기서 추가)
            if (target != null)
            {
                Entity targetEntity = target.GetComponent<Entity>();
                if (targetEntity != null)
                {
                    new DamageGiveEvent(
                        meleeDamage,
                        Vector3.zero,
                        this,
                        targetEntity,
                        new AtkTagSet().Add(AtkTags.physicalDamage),
                        1f
                    ).trigger();
                }
            }

            // recover
            if (meleeRecover > 0f)
                yield return new WaitForSeconds(meleeRecover);

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

        /* ---------- GRAB ---------- */
        private void StartGrab()
        {
            if (grabProjectilePrefab == null || grabSpawnPoint == null)
            {
                // 설정이 없으면 그냥 대기/추격으로 복귀
                state = CyberBruteState.Idle;
                currentAction = null;
                return;
            }

            grabTimer = grabCooldown;
            state = CyberBruteState.GrabAttack;
            currentAction = StartCoroutine(GrabShootRoutine());
        }

        private IEnumerator GrabShootRoutine()
        {
            if (target == null)
            {
                state = CyberBruteState.Idle;
                currentAction = null;
                yield break;
            }

            // 발사
            var grab = Instantiate(grabProjectilePrefab, grabSpawnPoint.position, Quaternion.identity);

            Vector2 dir = target.position - grabSpawnPoint.position;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            grab.Initialize(this, dir);

            // miss timeout: 맞으면 OnGrabHit에서 currentAction이 다른 루틴으로 갈아탄다
            float t = 0f;
            while (t < grabMissTimeout)
            {
                if (state != CyberBruteState.GrabAttack)
                    yield break;

                t += Time.deltaTime;
                yield return null;
            }

            // miss: recover
            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

        public void OnGrabHit(Entity grabbed)
        {
            if (state == CyberBruteState.Dead) return;
            if (grabbed == null) return;

            SpawnGrabText(grabbed);

            if (currentAction != null)
                StopCoroutine(currentAction);

            currentAction = StartCoroutine(GrabExecuteRoutine(grabbed));
        }

        private void SpawnGrabText(Entity targetEntity)
        {
            if (!grabTextPrefab || targetEntity == null) return;

            Vector3 pos = targetEntity.transform.position;

            Collider2D col = targetEntity.GetComponent<Collider2D>();
            if (col != null)
                pos.y += col.bounds.extents.y + 0.3f;
            else
                pos.y += 1.0f;

            Instantiate(grabTextPrefab, pos, Quaternion.identity);
        }

        private IEnumerator GrabExecuteRoutine(Entity grabbed)
        {
            state = CyberBruteState.GrabAttack;

            // 플레이어 락
            if (grabbed is Player p)
                p.BeginGrabLock(grabPullDuration + 0.2f);

            Vector2 start = grabbed.transform.position;
            float facing = Mathf.Sign(transform.localScale.x == 0 ? 1f : transform.localScale.x);
            Vector2 end = (Vector2)transform.position + new Vector2(grabPullOffsetX * facing, 0f);

            float t = 0f;
            while (t < grabPullDuration)
            {
                t += Time.deltaTime;
                grabbed.transform.position = Vector2.Lerp(start, end, t / grabPullDuration);
                yield return null;
            }


            new DamageGiveEvent(
                meleeDamage*10,
                Vector3.zero,
                this,
                grabbed,
                new AtkTagSet().Add(AtkTags.physicalDamage),
                1f
            ).trigger();

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

        /* ---------- MISSILE ---------- */
        private void StartMissileVolley()
        {
            if (missilePrefab == null || missileSpawnPoint == null)
            {
                state = CyberBruteState.Idle;
                currentAction = null;
                return;
            }

            missileTimer = missileCooldown;
            state = CyberBruteState.MissileVolley;
            currentAction = StartCoroutine(MissileRoutine());
        }

        private IEnumerator MissileRoutine()
        {
            if (target == null)
            {
                state = CyberBruteState.Idle;
                currentAction = null;
                yield break;
            }

            int count = Random.Range(minMissiles, maxMissiles + 1);

            for (int i = 0; i < count; i++)
            {
                if (target == null) break;

                var proj = Instantiate(missilePrefab, missileSpawnPoint.position, Quaternion.identity);
                Vector2 dir = target.position - missileSpawnPoint.position;
                if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

                proj.Initialize(this, dir, missilePower, missileSize);
                yield return new WaitForSeconds(missileInterval);
            }

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

        /* ---------- JUMP (FAST UP → HOVER → SLAM) ---------- */
        private void StartJumpAttack()
        {
            if (jumpInProgress || jumpWaitingAnim) return;

            jumpTimer = jumpCooldown;
            state = CyberBruteState.JumpAttack;

            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 1f;

            jumpWaitingAnim = true;

            Debug.Log("[Jump] Play jump animation");
            animator.Play("Jump"); // ← 점프 애니메이션 (이륙까지만 있음)
        }

        public void AnimEvent_JumpAnimFinished()
        {
            if (!jumpWaitingAnim) return;

            Debug.Log("[Jump] Animation finished → start physics jump");

            jumpWaitingAnim = false;
            jumpInProgress = true;

            StartCoroutine(JumpPhysicsRoutine());
        }



        private IEnumerator JumpPhysicsRoutine()
        {
            // 1) 빠른 상승
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, jumpUpSpeed);
            Debug.Log("[Jump] Up");

            yield return new WaitForSeconds(0.25f);

            // 2) 체공
            rb.linearVelocity = Vector2.zero;
            Debug.Log("[Jump] Hover");

            yield return new WaitForSeconds(hoverTime);

            // 3) 타겟 x축 보정
            if (target)
                rb.position = new Vector2(target.position.x, rb.position.y);

            // 4) 급강하
            rb.gravityScale = 1f;
            rb.linearVelocity = new Vector2(0f, -slamDownSpeed);
            Debug.Log("[Jump] Slam");

            // 5) 착지 대기
            while (!IsGrounded())
                yield return null;

            Debug.Log("[Jump] Landed");

            rb.linearVelocity = Vector2.zero;

            if (landingAoEPrefab && TryGetGroundPoint(out Vector2 groundPoint))
            {
                Vector3 spawnPos = new Vector3(
                    rb.position.x,
                    groundPoint.y,
                    0f
                );

                Instantiate(landingAoEPrefab, spawnPos, Quaternion.identity);
            }


            EndJump();
        }


        private bool IsGrounded()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (!col) return false;

            Vector2 origin = (Vector2)transform.position
                + Vector2.down * (col.bounds.extents.y + 0.05f);

            return Physics2D.Raycast(origin, Vector2.down, 0.15f, groundMask);
        }

        private void EndJump()
        {
            jumpInProgress = false;

            state = CyberBruteState.Recover;
            StartCoroutine(RecoverRoutine());

            Debug.Log("[Jump] End → Recover");
        }

        private bool TryGetGroundPoint(out Vector2 groundPoint)
        {
            groundPoint = Vector2.zero;

            Collider2D col = GetComponent<Collider2D>();
            if (!col) return false;

            Vector2 origin = (Vector2)transform.position;
            float rayLength = col.bounds.extents.y + 0.3f;

            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                Vector2.down,
                rayLength,
                groundMask
            );

            if (hit.collider != null)
            {
                groundPoint = hit.point;
                return true;
            }

            return false;
        }



        /* ---------- RECOVER ---------- */
        private IEnumerator RecoverRoutine()
        {
            yield return new WaitForSeconds(recoverDuration);
            currentAction = null;
            if (!isDead)
                state = CyberBruteState.Idle;
        }

        /* ===================== DEATH ===================== */
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

            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = false;

            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = Color.gray;

            Destroy(gameObject, deathDestroyDelay);
        }
    }
}
