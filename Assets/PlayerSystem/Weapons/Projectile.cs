using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>
    /// Basic projectile used by the player and enemies.
    /// Single-hit projectile: deals damage once and is destroyed.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private bool destroyOnAnyCollision = true;
        [SerializeField] private float size = 1f;

        private Vector2 direction;
        private float remainingLife;

        private float powerMultiplier = 1f;
        private float damageBonusPercent = 0f;
        private float knockbackForce = 0f;
        private float recoilForce = 0f;

        private IStat stat;
        private AtkTagSet atkTag =
            new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);

        // ✅ 핵심 플래그
        private bool hasHit = false;

        private void Awake()
        {
            remainingLife = lifeTime;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            transform.position += (Vector3)(direction * speed * dt);

            remainingLife -= dt;
            if (remainingLife <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(Entity owner, Vector2 direction, float power, float size)
        {
            stat = owner.stat.calculate();

            this.direction = direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;

            powerMultiplier = Mathf.Max(0.1f, power);
            remainingLife = lifeTime;

            transform.localScale += new Vector3(size, size, 0f);

            damageBonusPercent = 0f;
            knockbackForce = 0f;
            recoilForce = 0f;

            hasHit = false;

            atkTag = new AtkTagSet().Add(AtkTags.physicalDamage);
        }

        public void ApplyTriggerEnhancements(float bonusPercent, float knockback, float recoil)
        {
            damageBonusPercent += bonusPercent;
            knockbackForce += knockback;
            recoilForce += recoil;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // ✅ 이미 히트했으면 무조건 무시
            if (hasHit) return;

            if (((1 << other.gameObject.layer) & collisionMask) == 0)
                return;

            var entity = other.GetComponentInParent<Entity>();
            if (entity != null && entity != stat.entity)
            {
                hasHit = true;

                var tag = new AtkTagSet(atkTag);

                int damage = stat.calculateTrueDamage(tag, baseDamage);

                new DamageGiveEvent(
                    damage,
                    Vector3.zero,
                    stat.entity,
                    entity,
                    tag,
                    1
                ).trigger();

                Destroy(gameObject);
                return;
            }

            // 엔티티는 아니지만 충돌 시 파괴 옵션이 켜져 있으면 제거
            if (destroyOnAnyCollision)
            {
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}
