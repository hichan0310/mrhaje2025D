using UnityEngine;
using EntitySystem;

namespace PlayerSystem.Effects
{
    [CreateAssetMenu(menuName = "Player/Trigger Effects/Gain Resource", fileName = "GainResourceEffect")]
    public class GainResourceEffectAsset : TriggerEffectAsset
    {
        [SerializeField] private MemoryResourceType resourceType = MemoryResourceType.Energy;
        [SerializeField] private float amountPerPower = 5f;

        protected override void OnTrigger(Entity entity, float power)
        {
            if (!entity) return;

            float amount = amountPerPower * Mathf.Max(0f, power);

            if (MemoryTriggerContext.TryGetActive(entity, out var context))
            {
                context.Board.AddResource(resourceType, amount);
                return;
            }

            if (entity.TryGetComponent(out PlayerMemoryBinder memoryComponent) && memoryComponent.ActiveBoard != null)
            {
                memoryComponent.ActiveBoard.AddResource(resourceType, amount);
            }
        }
    }
}
