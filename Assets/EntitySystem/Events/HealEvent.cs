namespace EntitySystem.Events
{
    public class HealEvent: EventArgs
    {
        public int healAmount;
        public Entity entity;

        public HealEvent(Entity entity, int healAmount)
        {
            name = "HealEvent";
            this.healAmount = healAmount;
            this.entity = entity;
        }
        
        public override void trigger()
        {
            entity.takeHeal(this);
        }
    }
}