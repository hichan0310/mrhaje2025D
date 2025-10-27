namespace EntitySystem.Events
{
    public class SkillExecuteEvent:EventArgs
    {
        public Entity entity;
        //Todo:public ??? skill;

        public SkillExecuteEvent(Entity entity)
        {
            name=$"SkillExecuteEvent: {entity.name}";
            this.entity = entity;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}