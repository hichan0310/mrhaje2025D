using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;

namespace PlayerSystem.Polyominoes.HeatEnergyRelease
{
    public class EnergyBullet:SkillEffect
    {
        private float speed = 6f;                 // 항상 유지할 이동 속도
        private float maxTurnRateDeg = 1000f;      // 초당 최대 회전 각도
        private float retargetInterval = 0.2f;    // 목표 재탐색 주기

        private float retargetTimer;
        private Entity target;
        public DamageGiveEvent damageGiveEvent { get; set; }

        public EnergyBulletHit hit;

        private void Start()
        {
            this.timer = 6;
        }


        protected override void update(float deltaTime)
        {
            this.timer -= deltaTime;
            if (this.timer <= 0) OnTriggerEnter2D(null);
            retargetTimer -= deltaTime;
            if (!target || !target.isActiveAndEnabled || retargetTimer <= 0f)
            {
                target = AcquireNearestTarget();
                retargetTimer = retargetInterval;
            }

            // 현재 각도
            float currentAngle = rigidbody2D != null ? rigidbody2D.rotation : transform.eulerAngles.z;
            float newAngle = currentAngle;

            // 목표가 있으면 목표 각도로 제한 회전
            if (target)
            {
                Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                    float delta = Mathf.DeltaAngle(currentAngle, desiredAngle);
                    float maxStep = maxTurnRateDeg * deltaTime;           // 이번 프레임에서 회전 가능한 최대치
                    float step = Mathf.Clamp(delta, -maxStep, maxStep);   // 갑자기 확 꺾이지 않도록 제한
                    newAngle = currentAngle + step;
                }
            }

            // 회전 적용 및 속도 유지
            Vector2 dir = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad));

            if (rigidbody2D)
            {
                rigidbody2D.MoveRotation(newAngle);
                rigidbody2D.linearVelocity = dir * speed;  // 속도 항상 일정
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, newAngle);
                transform.position += (Vector3)(dir * (speed * deltaTime));
            }
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (other)
            {
                var e = other.gameObject.GetComponent<Entity>();
                if (!e) return;
                if (e == this.damageGiveEvent.attacker) return;
            }

            var h=Instantiate(hit);
            h.transform.position = this.transform.position;
            
            h.damageGiveEvent = this.damageGiveEvent;
            
            Destroy(gameObject);
        }


        private Entity AcquireNearestTarget()
        {
            Entity nearest = null;
            float minDistSqr = float.PositiveInfinity;
            Vector2 myPos = transform.position;

            // 비활성은 제외, 파생 클래스 포함
            var enemies = FindObjectsOfType<Entity>(false);
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!e.isActiveAndEnabled) continue;
                if (e == this.damageGiveEvent.attacker) continue;

                float d = ((Vector2)e.transform.position - myPos).sqrMagnitude;
                if (d < minDistSqr)
                {
                    minDistSqr = d;
                    nearest = e;
                }
            }
            return nearest;
        }
    }
}