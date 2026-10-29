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
        private readonly List<IEntityEventListener> tickBuffer = new();
        private readonly Stack<List<IEntityEventListener>> eventBuffers = new();
        public IReadOnlyList<IEntityEventListener> Listeners => listeners;
        
        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            TimeManager.registrarEntity(this);
            if (hpBar)
            {
                hpBar = Instantiate(hpBar);
                hpBar.target = this.transform;
            }
        }

        protected virtual void Update()
        {
            if (stat == null) return;
            if (hpBar)
            {
                int maximum = this is PlayerSystem.Player player && player.statCache != null ? player.statCache.maxHp : stat.maxHp;
                hpBar.ratio = (float)stat.nowHp / Mathf.Max(1, maximum);
            }
            update(TimeManager.deltaTime);
        }

        protected virtual void update(float deltaTime)
        {
            tickBuffer.Clear();
            tickBuffer.AddRange(listeners);
            for (int i=tickBuffer.Count-1; i>=0; i--)
            {
                if (listeners.Contains(tickBuffer[i])) tickBuffer[i].update(deltaTime, this);
            }
            tickBuffer.Clear();
        }
        
        public void registerListener(IEntityEventListener listener)
        {
            if (listener != null && !listeners.Contains(listener)) this.listeners.Add(listener);
        }

        public void removeListener(IEntityEventListener listener)
        {
            this.listeners.Remove(listener);
        }
        

        public virtual void eventActive(EventArgs e)
        {
            if (e == null) return;
            var snapshot = eventBuffers.Count > 0 ? eventBuffers.Pop() : new List<IEntityEventListener>();
            snapshot.AddRange(listeners);
            try
            {
                for (int i=snapshot.Count-1; i>=0; i--)
                    if (listeners.Contains(snapshot[i])) snapshot[i].eventActive(e);
            }
            finally
            {
                snapshot.Clear();
                eventBuffers.Push(snapshot);
            }
        }

        public void takeDamage(DamageGiveEvent e)
        {
            if (stat == null || e == null || e.target != this) return;
            if (e.attacker is EnemyBase && this is EnemyBase)
                return; //일단 서로는 절대 안때리게
            var snapshot = stat.calculate();
            var dmg = snapshot.calculateTakenDamage(e.atkTags, e.trueDmg);
            stat.takeDamage(dmg);
            new DamageTakeEvent(dmg, e.attacker, this, e.atkTags).trigger();
        }

        public void takeHeal(HealEvent e)
        {
            if (stat == null || e == null) return;
            this.stat.nowHp += e.healAmount;
            int maximum = stat.calculate().maxHp;
            if (stat.nowHp >= maximum) stat.nowHp = maximum;
        }

        protected virtual void OnDestroy()
        {
            TimeManager.removerEntity(this);
            if (hpBar) Destroy(hpBar.gameObject);
            listeners.Clear();
        }
    }
}
