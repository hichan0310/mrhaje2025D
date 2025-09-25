using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace GameBackend
{
    public abstract class BuffStackLimited : MonoBehaviour, IBuff, IEntityEventListener
    {
        protected class StackManager
        {
            public float time { get; set; }
            public int stack { get; set; }
        }

        protected Dictionary<Entity, StackManager> targets = new();

        protected int getStack(Entity entity)
        {
            return targets[entity].stack;
        }

        protected abstract float defaultTime { get; }
        protected abstract int limitStack { get; }

        public abstract void applyBuff(IStat stat);

        public virtual void eventActive(EventArgs eventArgs)
        {
        }

        public virtual void registerTarget(Entity target, object args = null)
        {
            if (targets.ContainsKey(target))
            {
                targets[target].time = this.defaultTime;
            }
            else
            {
                targets.Add(target, new StackManager() { time = this.defaultTime, stack = 0 });
                target.registerListener(this);
                target.stat.registerBuff(this);
            }

            if (targets[target].stack < limitStack) targets[target].stack += 1;
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
            targets[entity].time -= deltaTime;
            if (targets[entity].time <= 0)
            {
                if (targets[entity].stack >= 1)
                {
                    targets[entity].stack -= 1;
                    targets[entity].time = this.defaultTime;
                }
                else
                {
                    removeTarget(entity);
                }
            }
        }

        public abstract bool isStable { get; }
        public IStat targetStat { get; set; }
    }
}