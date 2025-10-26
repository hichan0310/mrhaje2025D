using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Effects
{
    /// <summary>
    /// Runtime component that applies a temporary attack buff.
    /// </summary>
    public class TemporaryStatModifier : MonoBehaviour
    {
        private Entity target;
        private float attackIncrease;
        private float remainingTime;
        private bool applied;

        public void Initialize(Entity entity, float attackIncreasePercent, float duration)
        {
            target = entity;
            attackIncrease = attackIncreasePercent;
            remainingTime = duration;
            Apply();
        }

        private void Update()
        {
            if (!target)
            {
                Destroy(this);
                return;
            }

            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            Remove();
        }

        private void Apply()
        {
            if (applied || !target)
            {
                return;
            }

            target.stat.increaseAtk += attackIncrease;
            applied = true;
        }

        private void Remove()
        {
            if (!applied || !target)
            {
                return;
            }

            target.stat.increaseAtk -= attackIncrease;
            applied = false;
        }
    }
}
