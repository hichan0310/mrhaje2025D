// Assets/EntitySystem/Events/DropDownEvent.cs
using EntitySystem;

namespace EntitySystem.Events
{
    /// <summary>
    /// 발판 관통(하강) 시작 시 발행하는 간단 이벤트.
    /// PowerPolicies.Select가 IPercentInfo.percent(=1) 값을 power로 사용합니다.
    /// </summary>
    public sealed class DropDownEvent : EventArgs, IEntityInfo, IPercentInfo
    {
        public Entity entity { get; }
        public float percent { get; }

        public DropDownEvent(Entity entity, float percent = 1f)
        {
            this.name = $"DropDownEvent: {entity?.name}";
            this.entity = entity;
            this.percent = percent <= 0f ? 1f : percent;
        }

        public override void trigger()
        {
            // 프로젝트의 다른 이벤트와 동일한 호출 방식
            entity?.eventActive(this);
        }
    }
}
