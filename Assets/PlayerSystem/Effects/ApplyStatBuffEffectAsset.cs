using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Effects
{
    [CreateAssetMenu(menuName = "Player/Effects/ApplyStatBuff")]
    public class ApplyStatBuffEffectAsset : TriggerEffectAsset
    {
        [SerializeField] private float attackPercent = 20f;
        [SerializeField] private float duration = 5f;

        protected override void OnTrigger(Entity entity, float power)
        {
            if (!entity) return;

            var buff = entity.gameObject.AddComponent<TemporaryStatModifier>();
            buff.Initialize(entity, attackPercent * power * 0.01f, duration);
        }
    }
}

