namespace EntitySystem.Events
{
    public class EntityDieEvent:EventArgs
    {
        public Entity attacker { get; }
        public Entity entity { get; }
        public EntityDieEvent(Entity entity, Entity attacker)
        {
            name="EntityDieEvent";
            this.entity = entity;
            this.attacker = attacker;
        }

        public override void trigger()
        {
            entity.eventActive(this);
            attacker.eventActive(this);
        }
    }
}