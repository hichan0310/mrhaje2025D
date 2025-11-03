namespace EntitySystem.Events
{
    public class JumpEvent:EventArgs
    {
        public Entity entity;
        public float jumpPower;

        public JumpEvent(Entity entity, float jumpPower)
        {
            name=$"JumpEvent: {entity.name}";
            this.entity = entity;
            this.jumpPower=jumpPower;
        }
        
        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}