using PlayerSystem.Weapons;
using UnityEngine;
using EntitySystem;

namespace PlayerSystem.Effects
{
    [CreateAssetMenu(menuName = "Player/Trigger Effects/Spawn Projectile", fileName = "SpawnProjectileEffect")]
    public class SpawnProjectileEffectAsset : TriggerEffectAsset
    {
        [SerializeField] private Projectile projectilePrefab = null;
        [SerializeField] private Vector2 spawnOffset = new Vector2(0.5f, 0.1f);
        [SerializeField] private float directionOverride = 0f;
        [SerializeField] private int projectileCount = 1;
        [SerializeField] private float spreadAngle = 5f;
        [SerializeField] private float size = 0f;

        protected override void OnTrigger(Entity entity, float power)
        {
            if (!projectilePrefab || !entity)
            {
                return;
            }

            MemoryTriggerContext.TryGetActive(entity, out var context);

            float angleStep = projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f;
            float startAngle = -spreadAngle * 0.5f;
            Vector2 baseDirection = entity.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
            if (directionOverride != 0f)
            {
                float radians = directionOverride * Mathf.Deg2Rad;
                baseDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                Vector2 direction = rotation * baseDirection;
                var instance = Instantiate(projectilePrefab, entity.transform.position + (Vector3)spawnOffset, Quaternion.identity);
                instance.Initialize(entity, direction, power, size);
                context?.ApplyToProjectile(instance);
            }
        }
    }
}
