namespace EntitySystem.Events
{
    public class JustDodgeEvent:EventArgs
    {
        public Entity entity;

        public JustDodgeEvent(Entity entity)
        {
            name = $"DodgeEvent: {entity.name}";
            this.entity = entity;
        }
        
        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}