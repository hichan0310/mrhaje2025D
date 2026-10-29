using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using Unity.VisualScripting;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Effects.AtkUpEffect
{
    public class AtkUpByPower: MonoBehaviour, ITriggerEffect
    {
        private readonly AtkUpBuff atkUpBuff = new AtkUpBuff();

        private void Start()
        {
        }

        public void trigger(Entity entity, float power)
        {
            atkUpBuff.registerTarget(entity);
            if(atkUpBuff.power<power) atkUpBuff.power = power;
        }

        private class AtkUpBuff:IBuff, IEntityEventListener
        {
            public bool isStable => false;
            
            public Entity target;
            public float power;
            private float time;
            
            public void applyBuff(IStat stat)
            {
                if (stat is EntityStat entityStat)
                {
                    entityStat.increaseAtk += power * 10;
                }
            }

            public void eventActive(EventArgs eventArgs)
            {
                
            }
            
            
            public void registerTarget(Entity target, object args = null)
            {
                if (this.target != target)
                {
                    removeSelf();
                    power = 0;
                }
                this.target = target;
                target.registerListener(this);
                target.stat.registerBuff(this);
                this.time = 10;
            }

            public void removeSelf()
            {
                if (target)
                {
                    target.removeListener(this);
                    target.stat.removeBuff(this);
                }
                target = null;
                power = 0;
            }

            public void update(float deltaTime, Entity target)
            {
                if (target != this.target) return;
                time -= deltaTime;
                if (time <= 0) removeSelf();
            }
        }
    }
}
