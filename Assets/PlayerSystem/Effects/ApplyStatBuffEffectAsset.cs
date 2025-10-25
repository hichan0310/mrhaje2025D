using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Effects
{
    [CreateAssetMenu(menuName = "Player/Trigger Effects/Stat Buff", fileName = "StatBuffEffect")]
    public class ApplyStatBuffEffectAsset : TriggerEffectAsset
    {
        [SerializeField] private float attackIncreasePercent = 15f;
        [SerializeField] private float duration = 3f;

        protected override void OnTrigger(Entity entity, float power)
        {
            if (!entity)
            {
                return;
            }

            if (MemoryTriggerContext.TryGetActive(entity, out var context))
            {
                context.AddDamageBonusPercent(attackIncreasePercent * power);
                return;
            }

            var buff = entity.gameObject.AddComponent<TemporaryStatModifier>();
            buff.Initialize(entity, attackIncreasePercent * power, duration);
        }
    }
}
