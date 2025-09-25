using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace EntitySystem.BuffTypes
{
    public abstract class Buff : MonoBehaviour, IBuff, IEntityEventListener
    {
        protected Entity target;

        public abstract void applyBuff(IStat stus);

        public virtual void eventActive(EventArgs eventArgs)
        {
        }

        public virtual void update(float deltaTime, Entity entity)
        {
        }

        public virtual void registerTarget(Entity target, object args=null)
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