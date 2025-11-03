using System.Collections.Generic;
using System.Linq;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using GameBackend;
using UnityEngine;

namespace EntitySystem
{
    public class Entity : MonoBehaviour
    {
        public EntityStat stat;
        public HpBar.HpBar hpBar;
        public Animator animator { get; private set; }
        
        private List<IEntityEventListener> listeners = new List<IEntityEventListener>();
        
        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            TimeManager.registrarEntity(this);
            hpBar = Instantiate(hpBar);
            hpBar.target = this.transform;
        }

        protected virtual void Update()
        {
            hpBar.ratio=(float)stat.nowHp/stat.maxHp;
            update(TimeManager.deltaTime);
        }

        protected virtual void update(float deltaTime)
        {
            foreach (var listener in listeners)
            {
                listener.update(deltaTime, this);
            }
        }
        
        public void registerListener(IEntityEventListener listener)
        {
            this.listeners.Add(listener);
        }

        public void removeListener(IEntityEventListener listener)
        {
            this.listeners.Remove(listener);
        }
        

        public virtual void eventActive(EventArgs e)
        {
            foreach (var listener in listeners.ToList())
            {
                listener.eventActive(e);
            }
        }

        public void takeDamage(DamageGiveEvent e)
        {
            if (stat == null || e == null || e.target != this) return;

            var snapshot = stat.calculate();
            var dmg = snapshot.calculateTakenDamage(e.atkTags, e.trueDmg);
            stat.takeDamage(dmg);
            new DamageTakeEvent(dmg, e.attacker, this, e.atkTags).trigger();
        }
    }
}
