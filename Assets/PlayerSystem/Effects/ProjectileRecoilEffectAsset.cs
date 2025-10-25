using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Effects
{
    [CreateAssetMenu(menuName = "Player/Trigger Effects/Projectile Recoil", fileName = "ProjectileRecoilEffect")]
    public class ProjectileRecoilEffectAsset : TriggerEffectAsset
    {
        [SerializeField] private float recoilForce = 3f;
        [SerializeField] private bool scaleWithPower = false;

        protected override void OnTrigger(Entity entity, float power)
        {
            if (!entity)
            {
                return;
            }

            if (MemoryTriggerContext.TryGetActive(entity, out var context))
            {
                float force = recoilForce;
                if (scaleWithPower)
                {
                    force *= Mathf.Max(0f, power);
                }

                context.AddRecoilForce(force);
            }
        }
    }
}
