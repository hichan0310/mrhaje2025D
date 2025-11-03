using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
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
        private IStat stat;
        private AtkTagSet atkTag;

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
            stat = this.owner.stat.calculate();
            this.owner = owner;
            this.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            this.powerMultiplier = Mathf.Max(0.1f, power);
            remainingLife = lifeTime;
            transform.localScale += new Vector3(size, size, 0);
            damageBonusPercent = 0f;
            knockbackForce = 0f;
            recoilForce = 0f;
            
            atkTag=new AtkTagSet().Add(AtkTags.physicalDamage);
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
                // calculateTrueDamage를 하면 tag에 criticalHit 추가될 수 있어서 복제해둬야 합니다 
                var tag = new AtkTagSet(this.atkTag);   
                // 공격력, 피해증가, 크리티컬 등 자동 적용됩니다
                int damage = stat.calculateTrueDamage(tag, 100);
                // projectile 형식이 아닌 공격이 들어올 수 있어서 저스트 회피는 이 안에서 처리하게 바꿔놨어요
                new DamageGiveEvent(damage, Vector3.zero, owner, entity, tag).trigger();

                // 넉백은 DamageGiveEvent의 force에서 전달만 하고 target에서 알아서 처리하게 하기
                // if (knockbackForce > 0f && entity.TryGetComponent(out Rigidbody2D targetBody))
                // {
                //     targetBody.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
                // }
                
                // 이건 뭐지?
                // if (recoilForce > 0f && owner && owner.TryGetComponent(out Rigidbody2D ownerBody))
                // {
                //     ownerBody.AddForce(-direction * recoilForce, ForceMode2D.Impulse);
                // }
            }

            if (destroyOnAnyCollision)
            {
                Destroy(gameObject);
            }
        }
    }
}
