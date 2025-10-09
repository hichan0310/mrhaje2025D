using System;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Runtime resource pool used by the memory board. Pieces can consume these resources instead of cooldowns.
    /// </summary>
    [Serializable]
    public class MemoryResourcePool
    {
        [SerializeField] private MemoryResourceType resourceType = MemoryResourceType.None;
        [SerializeField] private float maxAmount = 100f;
        [SerializeField] private float regenPerSecond = 5f;
        [SerializeField] private float startAmount = 0f;

        private float currentAmount;

        public MemoryResourceType ResourceType => resourceType;
        public float MaxAmount => maxAmount;
        public float RegenPerSecond => regenPerSecond;
        public float CurrentAmount => currentAmount;

        public void Initialize()
        {
            currentAmount = Mathf.Clamp(startAmount, 0f, maxAmount);
        }

        internal void ForceSetup(MemoryResourceType type, float max, float regen)
        {
            resourceType = type;
            maxAmount = Mathf.Max(0f, max);
            regenPerSecond = Mathf.Max(0f, regen);
            startAmount = Mathf.Min(startAmount, maxAmount);
            Initialize();
        }

        public void Tick(float deltaTime)
        {
            if (regenPerSecond <= 0f) return;
            currentAmount = Mathf.Min(maxAmount, currentAmount + regenPerSecond * deltaTime);
        }

        public bool TryConsume(float cost)
        {
            if (resourceType == MemoryResourceType.None || cost <= 0f)
            {
                return true;
            }

            if (currentAmount < cost)
            {
                return false;
            }

            currentAmount -= cost;
            return true;
        }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            currentAmount = Mathf.Min(maxAmount, currentAmount + amount);
        }
    }
}
