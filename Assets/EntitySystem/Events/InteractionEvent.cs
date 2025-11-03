using PlayerSystem;

namespace EntitySystem.Events
{
    public class InteractionEvent:EventArgs
    {
        public Entity entity;
        public IInteractable interactable;

        public InteractionEvent(Entity entity, IInteractable interactable)
        {
            this.entity = entity;
            this.interactable = interactable;
        }
        
        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}