using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Effects
{
    /// <summary>
    /// Base class used to define trigger effects as ScriptableObjects.
    /// </summary>
    public abstract class TriggerEffectAsset : ScriptableObject, ITriggerEffect
    {
        [Tooltip("Optional description used in the editor UI.")]
        [TextArea]
        [SerializeField] private string description = string.Empty;

        public string Description => description;

        public void trigger(Entity entity, float power)
        {
            OnTrigger(entity, power);
        }

        protected abstract void OnTrigger(Entity entity, float power);
    }
}
