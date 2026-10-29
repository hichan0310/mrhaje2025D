namespace EntitySystem.Events
{
    /// <summary>Attacker notification after a hit actually removed health. DamageGiveEvent remains the pre-hit modifier phase.</summary>
    public sealed class DamageDealtEvent : EventArgs
    {
        public Entity attacker { get; }
        public Entity target { get; }
        public int realDmg { get; }
        public AtkTagSet atkTags { get; }
        public DamageDealtEvent(Entity attacker, Entity target, int damage, AtkTagSet tags)
        {
            name = "DamageDealtEvent";
            this.attacker = attacker; this.target = target; realDmg = damage;
            atkTags = tags == null ? AtkTagSet.None : new AtkTagSet(tags);
        }
        public override void trigger()
        {
            if (attacker && realDmg > 0) attacker.eventActive(this);
        }
    }
}
