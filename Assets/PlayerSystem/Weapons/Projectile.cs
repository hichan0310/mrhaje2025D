using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>
    /// Basic projectile used by the player and enemies.
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

        private Entity owner;
        private Vector2 direction;
        private float remainingLife;
        private float powerMultiplier = 1f;
        private float damageBonusPercent = 0f;
        private float knockbackForce = 0f;
        private float recoilForce = 0f;

        private void Awake()
        {
            remainingLife = lifeTime;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            transform.position += (Vector3)(direction * (speed * deltaTime));
            remainingLife -= deltaTime;
            if (remainingLife <= 0f)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(Entity owner, Vector2 direction, float power, float size)
        {
            this.owner = owner;
            this.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            this.powerMultiplier = Mathf.Max(0.1f, power);
            remainingLife = lifeTime;
            transform.localScale += new Vector3(size, size, 0);
            damageBonusPercent = 0f;
            knockbackForce = 0f;
            recoilForce = 0f;
        }

        public void ApplyTriggerEnhancements(float bonusPercent, float knockback, float recoil)
        {
            damageBonusPercent += bonusPercent;
            knockbackForce += knockback;
            recoilForce += recoil;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & collisionMask) == 0)
            {
                return;
            }

            var entity = other.GetComponentInParent<Entity>();
            if (entity != null && entity != owner)
            {
                if (entity is PlayerSystem.Player player && player.TryInterceptAttack(owner))
                {
                    if (destroyOnAnyCollision)
                    {
                        Destroy(gameObject);
                    }
                    return;
                }

                var tags = new AtkTagSet();
                float totalPower = powerMultiplier * (1f + damageBonusPercent / 100f);
                int damage = Mathf.RoundToInt(baseDamage * Mathf.Max(0f, totalPower));
                new DamageGiveEvent(damage, Vector3.zero, owner, entity, tags).trigger();

                if (knockbackForce > 0f && entity.TryGetComponent(out Rigidbody2D targetBody))
                {
                    targetBody.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
                }

                if (recoilForce > 0f && owner && owner.TryGetComponent(out Rigidbody2D ownerBody))
                {
                    ownerBody.AddForce(-direction * recoilForce, ForceMode2D.Impulse);
                }
            }

            if (destroyOnAnyCollision)
            {
                Destroy(gameObject);
            }
        }
    }
}
