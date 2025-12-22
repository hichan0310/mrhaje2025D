using System.Collections.Generic;
using EntitySystem;
using EntitySystem.BuffTypes;
using UnityEngine;

namespace PlayerSystem.Polyominoes.FeatureExtractor
{
    public class FeatureExtractionExecutor : MonoBehaviour
    {
        [SerializeField] private CircleCollider2D circle;
        private static readonly Collider2D[] hits = new Collider2D[128];
        [SerializeField] private GameObject hitEffectPrefab;

        private Entity player;
        private float power;

        private void giveDamage()
        {
            if (!player)
            {
                Destroy(gameObject);
                return;
            }

            if (!circle)
                circle = GetComponent<CircleCollider2D>();

            float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Vector2 center = (Vector2)circle.bounds.center;

            // 레이어 필터 없이 전부 검사
            int count = Physics2D.OverlapCircleNonAlloc(center, radius, hits);

            var processed = new HashSet<Entity>();

            for (int i = 0; i < count; i++)
            {
                var col = hits[i];
                hits[i] = null;
                if (!col) continue;

                var e = col.GetComponent<Entity>();
                if (!e || e == player) continue;
                if (!processed.Add(e)) continue; // 콜라이더 여러 개 중복 방지


                // Entity에 추가한 IReadOnlyList 접근자 필요
                var listeners = e.Listeners;
                for (int j = 0; j < listeners.Count; j++)
                {
                    if (listeners[j] is DamageOverTime dot)
                    {
                        dot.giveDamage();
                        var noise = new Vector3(Random.value-0.5f, Random.value-0.5f, 0);
                        Destroy(Instantiate(hitEffectPrefab, e.transform.position+noise, Quaternion.identity), 0.5f);
                    }
                }

                Debug.Log(e.gameObject.name);
            }

            Destroy(gameObject, 0.1f);
        }

        public void init(Entity player, float power)
        {
            this.player = player;
            this.power = power;
            this.transform.position = player.transform.position;
            Invoke("giveDamage", 0.3f);
        }
    }
}