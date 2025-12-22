using EntitySystem;
using UnityEngine;

namespace EnemySystem.Projectiles
{
    [RequireComponent(typeof(Collider2D))]
    public class GrabProjectile : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float speed = 7f;
        [SerializeField] private float lifeTime = 2.5f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private Vector2 direction;
        private float timer;
        private CyberBruteController owner;

        private void Awake()
        {
            Debug.Log($"[GrabProjectile] Awake on {name}");
        }

        private void OnEnable()
        {
            Debug.Log($"[GrabProjectile] OnEnable on {name}");
        }

        public void Initialize(CyberBruteController owner, Vector2 dir)
        {
            this.owner = owner;
            direction = dir.sqrMagnitude > 0 ? dir.normalized : Vector2.right;
            timer = lifeTime;

            Debug.Log(
                $"[GrabProjectile] Initialize | owner={owner.name}, dir={direction}, lifeTime={lifeTime}"
            );
        }

        private void Update()
        {
            Vector3 before = transform.position;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            timer -= Time.deltaTime;

            Debug.Log(
                $"[GrabProjectile] Update | pos {before} -> {transform.position}, timer={timer:F2}"
            );

            if (timer <= 0f)
            {
                Debug.Log("[GrabProjectile] Lifetime expired → Destroy");
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log(
                $"[GrabProjectile] OnTriggerEnter2D | other={other.name}, layer={LayerMask.LayerToName(other.gameObject.layer)}"
            );

            // 1️ 레이어 마스크 체크
            if (((1 << other.gameObject.layer) & collisionMask) == 0)
            {
                Debug.Log("[GrabProjectile]  CollisionMask reject");
                return;
            }

            Debug.Log("[GrabProjectile]  CollisionMask pass");

            // 2️ Entity 추출
            var entity = other.GetComponentInParent<Entity>();
            if (entity == null)
            {
                Debug.Log("[GrabProjectile]  Entity not found in parent");
                return;
            }

            Debug.Log($"[GrabProjectile] Entity found: {entity.name}");

            // 3️ 자기 자신 체크
            if (entity == owner)
            {
                Debug.Log("[GrabProjectile]  Hit owner itself → ignored");
                return;
            }

            Debug.Log(
                $"[GrabProjectile] ALID HIT | entity={entity.name}, owner={owner.name}"
            );

            // 4️ 보스에게 전달
            Debug.Log("[GrabProjectile] Calling owner.OnGrabHit()");
            owner.OnGrabHit(entity);

            Debug.Log("[GrabProjectile] Destroy self after hit");
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            Debug.Log($"[GrabProjectile] OnDisable on {name}");
        }

        private void OnDestroy()
        {
            Debug.Log($"[GrabProjectile] OnDestroy on {name}");
        }
    }
}
