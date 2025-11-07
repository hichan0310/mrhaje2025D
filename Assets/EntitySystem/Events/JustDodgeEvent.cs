namespace EntitySystem.Events
{
    public class JustDodgeEvent : EventArgs, IEntityInfo, IDamageInfo
    {
        public Entity entity { get; }
        public DamageGiveEvent damageGiveEvent { get; }

        // 회피한 공격의 피해량을 damage로 노출
        public int damage => damageGiveEvent?.trueDmg ?? 0;

        public JustDodgeEvent(Entity entity, DamageGiveEvent damageGiveEvent)
        {
            name = $"JustDodgeEvent: {entity.name}";
            this.entity = entity;
            this.damageGiveEvent = damageGiveEvent;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}
