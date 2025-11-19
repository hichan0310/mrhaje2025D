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

        [Header("Melee")]
        [SerializeField] private string meleeTriggerName = "Melee";
        [SerializeField] private float meleeDuration = 0.6f;

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

        protected override void Start()
        {
            base.Start();

            // �� ���� ���� �⺻ ���� Ʃ��
            if (EnemyStat != null)
            {
                EnemyStat.armorType = ArmorType.SpecialArmor;
                EnemyStat.knockbackResist = 0.9f;          // �˹� ���� �� ����
                EnemyStat.contactDamageMultiplier = 2.0f; // �����ġ�� ���ϰ� ���� ������ ���
            }
        }

        // ===== AI ���� =====
        protected override void TickAI(float deltaTime)
        {
            if (state == CyberBruteState.Dead) return;
            if (target == null) return;

            // ��Ÿ�� ����
            if (meleeTimer > 0f) meleeTimer -= deltaTime;
            if (jumpTimer > 0f) jumpTimer -= deltaTime;
            if (missileTimer > 0f) missileTimer -= deltaTime;

            // ����/����/ȸ�� �� �׼� ���̸� ���� ����
            if (currentAction != null) return;

            decisionTimer -= deltaTime;
            if (decisionTimer > 0f) return;
            decisionTimer = decisionInterval;

            float dist = Vector2.Distance(transform.position, target.position);

            // ���� �켱
            if (dist <= meleeRange && meleeTimer <= 0f)
            {
                StartMelee();
                return;
            }

            // �߰Ÿ�: ���� or �̻���
            if (dist <= midRange && missileTimer <= 0f)
            {
                if (jumpTimer <= 0f && Random.value < 0.5f)
                    StartJumpAttack();
                else
                    StartMissileVolley();
                return;
            }

            // �߰� ����
            if (dist <= chaseRange)
            {
                state = CyberBruteState.Chase;
            }
            else
            {
                state = CyberBruteState.Idle;
            }
        }

        // ===== �̵�/���� =====
        protected override void TickMovement(float fixedDeltaTime)
        {
            if (rb == null) return;
            if (state == CyberBruteState.Dead) return;

            // ����/����/ȸ�� �߿��� �̵� X
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

        // ===== �׼ǵ� =====
        #region Actions

        private void StartMelee()
        {
            if (currentAction != null) return;

            meleeTimer = meleeCooldown;
            state = CyberBruteState.MeleeAttack;
            currentAction = StartCoroutine(MeleeRoutine());
        }

        private IEnumerator MeleeRoutine()
        {
            if (animator != null && !string.IsNullOrEmpty(meleeTriggerName))
            {
                animator.SetTrigger(meleeTriggerName);
            }

            // ��Ʈ�ڽ��� �ִϸ��̼� �̺�Ʈ���� DamageGiveEvent�� ��ų�,
            // ���⼭ Physics2D.OverlapCircle ������ ó���ص� ��.

            yield return new WaitForSeconds(meleeDuration);

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

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
            rb.gravityScale = 0f; // ������ ���� ���

            while (t < jumpDuration)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / jumpDuration);

                // �߰����� �ְ����� ������ Ŀ��
                float hCurve = 4f * n * (1f - n);
                Vector2 pos = Vector2.Lerp(startPos, endPos, n);
                pos.y += hCurve * jumpHeight;

                rb.MovePosition(pos);
                yield return null;
            }

            rb.gravityScale = originalGravity;

            // ���� ���� ����
            if (landingAoEPrefab != null)
            {
                Object.Instantiate(landingAoEPrefab, rb.position, Quaternion.identity);
            }

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

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

                // EnemyBase�� Entity�� ����ϹǷ� this�� �� owner
                proj.Initialize(this, dir, missilePower, missileSize);

                yield return new WaitForSeconds(missileInterval);
            }

            state = CyberBruteState.Recover;
            currentAction = StartCoroutine(RecoverRoutine());
        }

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

        // ===== ��� / �̺�Ʈ =====
        protected override void OnDie(Entity attacker)
        {
            if (state == CyberBruteState.Dead) return;

            state = CyberBruteState.Dead;

            // ���� ���� �׼� ����
            if (currentAction != null)
            {
                StopCoroutine(currentAction);
                currentAction = null;
            }
            StopAllCoroutines();

            // ����/�̵� ����
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            // �ݶ��̴� ��Ȱ��ȭ
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            // �״� �ִϸ��̼�
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            // TODO: ���, ����, ī�޶� ����ũ �� �߰� ����

            Destroy(gameObject, deathDestroyDelay);
        }

        protected override void OnEvent(EventArgs e)
        {
            // ���ϸ� ���⼭ DamageTakeEvent ���� �ǰ� ����Ʈ/����/�˹� ���� ����
            // var dmg = e as DamageTakeEvent;
            // if (dmg != null && dmg.target == this)
            // {
            //     // EnemyStat.knockbackResist �̿��ؼ� force ���̱� ���� �͡�
            // }
        }
    }
}
