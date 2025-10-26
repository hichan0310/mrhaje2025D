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
            Debug.Log($"[StatBuffEffect] Triggered → entity:{entity?.name ?? "null"}, power:{power}");

            if (!entity)
            {
                Debug.LogWarning("[StatBuffEffect] Entity is NULL → 버프 적용 불가");
                return;
            }

            float totalBuff = attackIncreasePercent * power;
            Debug.Log($"[StatBuffEffect] Calculated Buff = {attackIncreasePercent}% * {power} = {totalBuff}");

            if (MemoryTriggerContext.TryGetActive(entity, out var context))
            {
                Debug.Log($"[StatBuffEffect] MemoryTriggerContext 적용! → {entity.name}");
                context.AddDamageBonusPercent(totalBuff);
                Debug.Log($"[StatBuffEffect] context.AddDamageBonusPercent({totalBuff}) 호출됨");
                return;
            }
            else
            {
                Debug.LogWarning($"[StatBuffEffect] MemoryTriggerContext 없음 → TemporaryStatModifier로 처리");
            }

            var buff = entity.gameObject.AddComponent<TemporaryStatModifier>();
            if (buff == null)
            {
                Debug.LogError("[StatBuffEffect] TemporaryStatModifier 추가 실패!!");
            }
            else
            {
                Debug.Log($"[StatBuffEffect] TemporaryStatModifier 추가됨 → {entity.name}, Duration:{duration}");
            }

            buff.Initialize(entity, totalBuff, duration);
            Debug.Log($"[StatBuffEffect] buff.Initialize({entity.name}, {totalBuff}, {duration}) 호출됨");
        }
    }
}

