
using UnityEngine;

namespace EntitySystem.Events
{
    public class UltimateExecuteEvent:EventArgs
    {
        public Entity entity;
        public Vector3 targetPos;
        //Todo:public ??? skill;

        public UltimateExecuteEvent(Entity entity, Vector3 targetPos)
        {
            name=$"UltimateExecuteEvent: {entity.name}";
            this.entity = entity;
            this.targetPos = targetPos;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}