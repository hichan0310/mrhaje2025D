using System.Collections.Generic;
using EntitySystem.Events;
using EntitySystem.StatSystem;

namespace EntitySystem.BuffTypes
{
    public abstract class BuffStackIndependent : IBuff, IEntityEventListener
    {
        protected class StackManager
        {
            private int maxStack;
            private float[] time;
            private bool[] _stack;

            public int stack
            {
                get
                {
                    int s = 0;
                    for (int i = 0; i < maxStack; i++)
                        if (this._stack[i])
                            s++;
                    return s;
                }
            }

            public StackManager(int maxStack)
            {
                this.maxStack = maxStack;
                time = new float[maxStack];
                _stack = new bool[maxStack];
            }

            public void timeDecrease(float time)
            {
                for (int i = 0; i < maxStack; i++)
                {
                    if (_stack[i])
                    {
                        this.time[i] -= time;
                        if (this.time[i] < 0)
                        {
                            this.time[i] = 0;
                            _stack[i] = false;
                        }
                    }
                }
            }

            public void addStack(float time)
            {
                int minidx = 0;
                for (int i = 0; i < maxStack; i++)
                {
                    if (!_stack[i])
                    {
                        this._stack[i] = true;
                        this.time[i] = time;
                        return;
                    }
                    else
                    {
                        if (this.time[i] < this.time[minidx])
                            minidx = i;
                    }
                }

                if (this.time[minidx] < time)
                {
                    this.time[minidx] = time;
                }
            }
        }

        // 0: time, 1: stack
        protected Dictionary<Entity, StackManager> targets = new();

        protected abstract float defaultTime { get; }

        public abstract void applyBuff(IStat status);

        public abstract void eventActive<T>(T eventArgs) where T : EventArgs;

        public virtual void registrarTarget(Entity target, object args = null)
        {
            if (targets.ContainsKey(target)) return;

            this.targets[target].addStack((args is ITimeInfo t) ? t.time : this.defaultTime);
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

        protected void removeTarget(Entity target)
        {
            this.targets.Remove(target);
            target.removeListener(this);
            target.stat.removeBuff(this);
        }

        public virtual void update(float deltaTime, Entity entity)
        {
            if(!this.targets.ContainsKey(entity)) return;
            targets[entity].timeDecrease(deltaTime);
            if (targets[entity].stack == 0)
            {
                removeTarget(entity);
            }
        }

        public abstract bool isStable { get; }
        public IStat targetStat { get; set; }
    }
}