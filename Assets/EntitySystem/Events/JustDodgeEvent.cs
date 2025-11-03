namespace EntitySystem.Events
{
    public class JustDodgeEvent:EventArgs
    {
        public Entity entity;
        public DamageGiveEvent damageGiveEvent;

        public JustDodgeEvent(Entity entity, DamageGiveEvent damageGiveEvent)
        {
            name = $"DodgeEvent: {entity.name}";
            this.entity = entity;
            this.damageGiveEvent = damageGiveEvent;
        }
        
        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}