using UnityEngine;

namespace EntitySystem.Events
{
    public class HeavyAttackExecuteEvent:EventArgs
    {
        public Entity entity;
        public Vector3 targetPosition;

        public HeavyAttackExecuteEvent(Entity entity, Vector3 targetPosition)
        {
            name = $"HeavyAttackExecuteEvent: {entity.name}";
            this.entity = entity;
            this.targetPosition = targetPosition;
        }
        
        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}