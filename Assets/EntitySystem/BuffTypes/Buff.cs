using EntitySystem.Events;
using EntitySystem.StatSystem;

namespace EntitySystem.BuffTypes
{
    public abstract class Buff : IBuff, IEntityEventListener
    {
        protected Entity target;

        public virtual void applyBuff(IStat stus)
        {
        }

        public virtual void eventActive<T>(T eventArgs) where T : EventArgs
        {
        }

        public virtual void update(float deltaTime, Entity entity)
        {
        }

        public virtual void registrarTarget(Entity target, object args=null)
        {
            if (this.target != null)
            {
                removeSelf();
            }
            
            this.target = target;
            target.registerListener(this);
            target.stat.registerBuff(this);
        }

        public virtual void removeSelf()
        {
            target.removeListener(this);
            target.stat.removeBuff(this);
        }

        public abstract bool isStable { get; }
        public IStat targetStat { get; set; }
    }
}