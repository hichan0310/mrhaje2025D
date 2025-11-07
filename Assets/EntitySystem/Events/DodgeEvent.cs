namespace EntitySystem.Events
{
    public class DodgeEvent : EventArgs, IEntityInfo
    {
        public Entity entity { get; }

        public DodgeEvent(Entity entity)
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
