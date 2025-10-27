using UnityEngine;

namespace EntitySystem.Events
{
    public class BasicAttackExecuteEvent:EventArgs
    {
        public Entity entity;
        public Vector3 targetPosition;

        public BasicAttackExecuteEvent(Entity entity, Vector3 targetPosition)
        {
            name = $"BasicAttackExecuteEvent: {entity.name}";
            this.entity = entity;
            this.targetPosition = targetPosition;
        }
        
        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}