
using PlayerSystem;
using UnityEngine;

namespace EntitySystem.Events
{
    public class UltimateExecuteEvent:EventArgs
    {
        public Entity entity;
        public Ultimate skill;

        public UltimateExecuteEvent(Entity entity, Ultimate skill)
        {
            name=$"UltimateExecuteEvent: {entity.name}";
            this.entity = entity;
            this.skill = skill;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}