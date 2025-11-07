using PlayerSystem;

namespace EntitySystem.Events
{
    public class UltimateExecuteEvent : EventArgs, IEntityInfo, IPercentInfo
    {
        public Entity entity { get; }
        public Ultimate skill { get; }

        // 예: 소비 에너지/파워가 있다면 반영, 없으면 1f
        public float percent
        {
            get
            {
                return 1f;
            }
        }

        public UltimateExecuteEvent(Entity entity, Ultimate skill)
        {
            name = $"UltimateExecuteEvent: {entity.name}";
            this.entity = entity;
            this.skill = skill;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}
