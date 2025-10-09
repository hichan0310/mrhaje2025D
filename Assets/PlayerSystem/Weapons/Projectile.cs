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

        private Entity owner;
        private Vector2 direction;
        private float remainingLife;
        private float powerMultiplier = 1f;

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

        public void Initialize(Entity owner, Vector2 direction, float power)
        {
            this.owner = owner;
            this.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            this.powerMultiplier = Mathf.Max(0.1f, power);
            remainingLife = lifeTime;
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
                int damage = Mathf.RoundToInt(baseDamage * powerMultiplier);
                new DamageGiveEvent(damage, Vector3.zero, owner, entity, tags).trigger();
            }

            if (destroyOnAnyCollision)
            {
                Destroy(gameObject);
            }
        }
    }
}
