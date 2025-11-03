using System.Collections.Generic;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using Unity.VisualScripting;
using UnityEngine;

namespace EntitySystem.BuffTypes
{
    public abstract class BuffOnce : MonoBehaviour, IBuff, IEntityEventListener
    {
        
        protected Dictionary<Entity, float> targets = new();

        public abstract void applyBuff(IStat stat);

        public virtual void eventActive(EventArgs eventArgs){}

        public interface IHaveTime
        {
            public float time { get; }
        }
        public virtual void registerTarget(Entity target, object args=null)
        {
            if(targets.ContainsKey(target)) return;
            
            this.targets[target] = (args is IHaveTime t) ? t.time : -1;
            target.registerListener(this);
            target.stat.registerBuff(this);
        }

        public virtual void removeSelf()
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