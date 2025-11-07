namespace EntitySystem.Events
{
    public class EntityDieEvent : EventArgs, IEntityInfo
    {
        public Entity attacker { get; }
        public Entity entity { get; }   // IEntityInfo

        public EntityDieEvent(Entity entity, Entity attacker)
        {
            name = "EntityDieEvent";
            this.entity = entity;
            this.attacker = attacker;
        }

        public override void trigger()
        {
            entity?.eventActive(this);
            attacker?.eventActive(this);
        }
    }
}
