namespace EntitySystem.Events
{
    public class JumpEvent:EventArgs
    {
        public Entity entity;

        public JumpEvent(Entity entity)
        {
            name=$"JumpEvent: {entity.name}";
            this.entity = entity;
        }
        
        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}