// Assets/PlayerSystem/Effects/TripleShotEffectAsset.cs
using UnityEngine;
using EntitySystem;
using PlayerSystem.Weapons;   // ← Projectile 정의 네임스페이스
using PlayerSystem;          // ← MemoryTriggerContext.TryGetActive 사용

namespace PlayerSystem.Effects
{
    [CreateAssetMenu(menuName = "Player/Effects/Triple Shot", fileName = "TripleShotEffect")]
    public class TripleShotEffectAsset : TriggerEffectAsset
    {
        [Header("Projectile")]
        public Projectile projectilePrefab;

        [Header("Pattern")]
        [Min(1)] public int count = 3;
        [Range(0f, 180f)] public float spreadDegrees = 30f;

        [Header("Kinetics")]
        public float speed = 12f;
        public float lifetime = 5f;

        [Header("Damage (optional)")]
        public float baseDamage = 10f; // 프로젝트일 API에 맞추어 적용(주석 참고)

        protected override void OnTrigger(Entity entity, float power)
        {
            Debug.Log($"TripleShotEffect fired power={power}, count={count}");
            if (!entity || !projectilePrefab) return;

            // 발사 위치: Player의 자식 "FirePoint"가 있으면 우선 사용
            Vector3 spawnPos = entity.transform.position;
            var firePoint = entity.transform.Find("FirePoint");
            if (firePoint) spawnPos = firePoint.position;

            // 좌/우 바라보는 방향(스프라이트 스케일 기준), 기본은 오른쪽
            Vector3 forward = (entity.transform.localScale.x < 0f) ? Vector3.left : Vector3.right;

            // 중심에서 좌/우로 균등 분산
            float half = spreadDegrees * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float t = (count == 1) ? 0f : (float)i / (count - 1); // 0..1
                float angle = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.Euler(0, 0, angle) * forward;

                var proj = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                proj.Initialize(entity, dir.normalized, speed, lifetime);

                // 컨텍스트 보정(추가 데미지/넉백/반동 등)을 적용
                if (MemoryTriggerContext.TryGetActive(entity, out var ctx))
                    ctx.ApplyToProjectile(proj);

                // 프로젝트의 Projectile API에 따라 데미지 주입이 가능하면 여기서 적용하세요.
                //   예) proj.SetBaseDamage(baseDamage * power);
                //   또는  proj.damage = ...   (당신들의 구현에 맞춰 사용)
            }
        }
    }
}
