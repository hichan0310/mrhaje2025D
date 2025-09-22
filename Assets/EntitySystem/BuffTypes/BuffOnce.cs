using System.Collections.Generic;
using EntitySystem.Events;
using EntitySystem.StatSystem;

namespace EntitySystem.BuffTypes
{
    public abstract class BuffOnce : IBuff, IEntityEventListener
    {
        
        protected Dictionary<Entity, float> targets = new();

        public abstract void applyBuff(IStat status);

        public abstract void eventActive<T>(T eventArgs) where T : EventArgs;

        public virtual void registrarTarget(Entity target, object args=null)
        {
            if(targets.ContainsKey(target)) return;
            
            this.targets[target] = -1;
            target.registerListener(this);
            target.stat.registerBuff(this);
        }

        public void removeSelf()
        {
            foreach (var target in targets)
            {
                target.Key.removeListener(this);
                target.Key.stat.removeBuff(this);
            }
            targets.Clear();
        }

        protected virtual void removeTarget(Entity target)
        {
            target.removeListener(this);
            target.stat.removeBuff(this);
            this.targets.Remove(target);
        }

        public virtual void update(float deltaTime, Entity entity)
        {
            if (!this.targets.ContainsKey(entity)) return;
            if(targets[entity] <= -0.5f) return;
            targets[entity] -= deltaTime;
            if (targets[entity] <= 0) removeTarget(entity);
        }

        public abstract bool isStable { get; }
        public IStat targetStat { get; set; }
    }
}