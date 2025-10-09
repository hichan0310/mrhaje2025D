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

            var memoryComponent = entity.GetComponent<PlayerMemoryBinder>();
            if (!memoryComponent)
            {
                return;
            }

            memoryComponent.Board?.AddResource(resourceType, amountPerPower * Mathf.Max(0f, power));
        }
    }
}
