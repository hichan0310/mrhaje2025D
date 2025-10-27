namespace EntitySystem.Events
{
    public class UltimateExecuteEvent:EventArgs
    {
        public Entity entity;
        //Todo:public ??? skill;

        public UltimateExecuteEvent(Entity entity)
        {
            name=$"UltimateExecuteEvent: {entity.name}";
            this.entity = entity;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}