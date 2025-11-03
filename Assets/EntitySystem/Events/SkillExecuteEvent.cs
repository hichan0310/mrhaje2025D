using PlayerSystem;

namespace EntitySystem.Events
{
    public class SkillExecuteEvent:EventArgs
    {
        public Entity entity;
        public Skill skill;

        public SkillExecuteEvent(Entity entity, Skill skill)
        {
            name=$"SkillExecuteEvent: {entity.name}";
            this.entity = entity;
            this.skill = skill;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}