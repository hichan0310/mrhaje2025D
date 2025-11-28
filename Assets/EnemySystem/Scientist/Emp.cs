// Assets/EnemySystem/Scientist/EmpGrenade.cs
using UnityEngine;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;

namespace EnemySystem
{
    /// <summary>
    /// Simple EMP grenade:
    /// - Moves with Rigidbody2D (parabolic arc)
    /// - Explodes on collision or after lifetime
    /// - Deals damage and can later apply skill cooldown debuff.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class EmpGrenade : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private int damage = 10;
        [SerializeField] private LayerMask hitMask = ~0;

        [HideInInspector] public Entity owner;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifeTime)
            {
                Explode();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Explode();
        }

        private void Explode()
        {
            Vector2 center = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius, hitMask);

            for (int i = 0; i < hits.Length; i++)
            {
                Entity target = hits[i].GetComponentInParent<Entity>();
                if (target == null) continue;
                if (owner != null && target == owner) continue;

                // Damage
                var tags = new AtkTagSet()
                    .Add(AtkTags.physicalDamage); // you can change to electric-type if available

                new DamageGiveEvent(damage, center, owner, target, tags, 1).trigger();

                // TODO:
                //   Apply "skill cooldown increase" debuff using your buff system.
                //   For example, if you have IBuff and skillCooldownDecrease stat,
                //   you can create an EmpDebuff implementing IBuff and register it
                //   on target.stat.
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}

