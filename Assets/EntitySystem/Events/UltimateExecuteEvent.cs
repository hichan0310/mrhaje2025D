
using PlayerSystem;
using UnityEngine;

namespace EntitySystem.Events
{
    public class UltimateExecuteEvent:EventArgs
    {
        public Entity entity;
        public int energy;

        public UltimateExecuteEvent(Entity entity, int energy)
        {
            name=$"UltimateExecuteEvent: {entity.name}";
            this.entity = entity;
            this.energy = energy;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}