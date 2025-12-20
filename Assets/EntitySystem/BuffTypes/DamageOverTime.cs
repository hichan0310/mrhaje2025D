using EntitySystem.Events;

namespace EntitySystem.BuffTypes
{
    public abstract class DamageOverTime: IEntityEventListener
    {
        public Entity target;
        public DamageGiveEvent damageGiveEvent { get; set; }


        public abstract void eventActive(EventArgs eventArgs);

        public void registerTarget(Entity target, object args = null)
        {
            this.target = target;
            target.registerListener(this);
        }

        public void removeSelf()
        {
            target.removeListener(this);
        }

        public abstract void update(float deltaTime, Entity target);

        public void giveDamage()
        {
            if (damageGiveEvent is not null)
                if (damageGiveEvent.target == target)
                    this.damageGiveEvent.trigger();
        }
    }
}