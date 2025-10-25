using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Effects
{
    [CreateAssetMenu(menuName = "Player/Trigger Effects/Projectile Knockback", fileName = "ProjectileKnockbackEffect")]
    public class ProjectileKnockbackEffectAsset : TriggerEffectAsset
    {
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private bool scaleWithPower = true;

        protected override void OnTrigger(Entity entity, float power)
        {
            if (!entity)
            {
                return;
            }

            if (MemoryTriggerContext.TryGetActive(entity, out var context))
            {
                float force = knockbackForce;
                if (scaleWithPower)
                {
                    force *= Mathf.Max(0f, power);
                }

                context.AddKnockbackForce(force);
            }
        }
    }
}
