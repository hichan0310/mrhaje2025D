using PlayerSystem;

namespace EntitySystem.Events
{
    public class SkillExecuteEvent : EventArgs, IEntityInfo, IPercentInfo
    {
        public Entity entity { get; }
        public Skill skill { get; }

        // 스킬의 파워 기준이 있으면 연결, 없으면 1f
        public float percent
        {
            get
            {
                // 예: skill.power 같은 값이 있으면 사용
                // 현재 알 수 없으므로 기본 1f
                return 1f;
            }
        }

        public SkillExecuteEvent(Entity entity, Skill skill)
        {
            name = $"SkillExecuteEvent: {entity.name}";
            this.entity = entity;
            this.skill = skill;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}
