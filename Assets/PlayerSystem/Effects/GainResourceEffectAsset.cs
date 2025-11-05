using UnityEngine;
using EntitySystem;

namespace PlayerSystem.Effects
{
    /// <summary>
    /// Grants a specified amount of a memory resource to the entity (via PlayerMemoryBinder).
    /// NOTE: TriggerEffectAsset의 추상 메서드 OnTrigger(...)만 구현합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Memory/Effects/GainResource")]
    public class GainResourceEffectAsset : TriggerEffectAsset
    {
        [Header("Resource Settings")]
        [SerializeField] private MemoryResourceType resourceType = MemoryResourceType.None;
        [SerializeField] private float amountPerPower = 1f;

        // TriggerEffectAsset의 추상 멤버 구현 (trigger(...)를 override 하면 안 됩니다)
        protected override void OnTrigger(Entity entity, float power)
        {
            if (!entity) return;

            var binder = entity.GetComponent<PlayerSystem.PlayerMemoryBinder>();
            if (!binder)
            {
                Debug.LogWarning($"[GainResourceEffectAsset] {entity.name}에 PlayerMemoryBinder가 없습니다.");
                return;
            }

            float amount = Mathf.Max(0f, amountPerPower * power);
            if (amount <= 0f) return;

            binder.AddResource(resourceType, amount);
        }
    }
}
