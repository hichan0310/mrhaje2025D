using DefaultNamespace;
using EntitySystem.Events;
using UnityEngine;

namespace EntitySystem.BuffTypes
{
    public abstract class DamageOverTime: MonoBehaviour, IEntityEventListener
    {
        public Entity target { get; set; }
        public DamageGiveEvent damageGiveEvent { get; set; }
        protected float timer = 0;
        protected abstract float cooldown { get; }
        private int damageCount = 0;


        public virtual void eventActive(EventArgs eventArgs)
        {
            if(eventArgs is EntityDieEvent) Destroy(this.gameObject);
        }

        public virtual void registerTarget(Entity target, object args = null)
        {
            this.target = target;
            target.registerListener(this);
        }

        public virtual void removeSelf()
        {
            target.removeListener(this);
        }

        public virtual void update(float deltaTime, Entity target)
        {
            if(this.target != target) return;
            timer+=deltaTime;
            if (timer > cooldown)
            {
                timer-=cooldown;
                damageCount++;
                giveDamage();
            }
        }

        public void giveDamage()
        {
            if (damageGiveEvent is not null)
                if (damageGiveEvent.target == target)
                    this.damageGiveEvent.trigger();
        }
        
        private void OnDestroy()
        {
            // Destroy로 사라질 때도 정리
            if (target != null)
                removeSelf();
        }
    }
}