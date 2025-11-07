namespace EntitySystem.Events
{
    public class DamageTakeEvent : EventArgs, IEntityInfo, IDamageInfo
    {
        public int realDmg { get; set; }
        public Entity attacker { get; }
        public Entity target { get; }
        public AtkTagSet atkTags { get; }

        // IEntityInfo
        public Entity entity => target;

        // IDamageInfo
        public int damage => realDmg;

        public DamageTakeEvent(int realDmg, Entity attacker, Entity target, AtkTagSet atkTags)
        {
            name = "DmgTakeEvent";
            this.realDmg = realDmg;
            this.attacker = attacker;
            this.target = target;
            this.atkTags = atkTags;
        }

        public override void trigger()
        {
            if (atkTags != null && atkTags.Contains(AtkTags.notTakeEvent)) return;

            DamageEventManager.Instance?.TriggerDmgTakeEvent(this);
            target?.eventActive(this);

            if (target != null && target.stat != null && target.stat.nowHp <= 0)
            {
                new EntityDieEvent(target, attacker).trigger();
            }
        }
    }
}
