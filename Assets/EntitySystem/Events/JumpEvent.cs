
namespace EntitySystem.Events
{
    public class JumpEvent : EventArgs, IEntityInfo, IPercentInfo
    {
        public Entity entity { get; }
        public float jumpPower { get; }

        // IPercentInfo: 점프 파워 그대로 사용 (필요시 스케일링 규칙을 PowerPolicies에서)
        public float percent => jumpPower;

        public JumpEvent(Entity entity, float jumpPower)
        {
            name = $"JumpEvent: {entity.name}";
            this.entity = entity;
            this.jumpPower = jumpPower;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}
