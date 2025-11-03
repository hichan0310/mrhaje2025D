using System.Collections.Generic;
using EntitySystem;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.UltimateSkills.DamageEndingSniper
{
    public class SnipeBullet : MonoBehaviour
    {
        public Vector3 target { get; set; }
        public SnipeHitEffect snipeEffect;
        public DamageEndingSniper snipe { get; set; }
        public IStat stat { get; set; }
        public Dictionary<Entity, DisplayStack> stack { get; set; }

        private float speed = 300f;

        Rigidbody2D rb;
        Vector2 dir;
        float timer;
        
        [SerializeField] List<GameObject> notTrails;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // 목표 방향 계산
            Vector2 from = transform.position;
            Vector2 to = target;
            dir = (to - from).normalized;

            // 속도 설정
            rb.linearVelocity = dir * speed;

            // 방향 바라보기
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // 도착 타이머
            timer = Vector2.Distance(from, to) / speed;
        }
        
        private bool destroyed = false;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f && !destroyed)
            {
                if (snipeEffect)
                {
                    var s=Instantiate(snipeEffect, target, Quaternion.identity);
                    s.stat = stat;
                    s.stack = stack;
                }
                rb.linearVelocity = Vector2.zero;
                snipe.finishBullet();
                foreach (var notT in notTrails)
                {
                    Destroy(notT);
                }
                Destroy(gameObject, 1.2f);
                rb.linearVelocity = Vector2.zero;
                destroyed = true;
            }
        }
    }
}