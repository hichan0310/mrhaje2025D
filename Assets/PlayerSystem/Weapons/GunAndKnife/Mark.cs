using System.Collections.Generic;
using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class Mark:MonoBehaviour, IEntityEventListener
    {
        [SerializeField] private float duration;
        
        public class Marking
        {
            public float time;
            public int requireHit;
            public GameObject effect;

            public Marking(float time, int requireHit, GameObject effect)
            {
                this.time = time;
                this.requireHit = requireHit;
                this.effect = effect;
            }
        }
        
        public Player player { get; set; }
        private Dictionary<Entity, Marking> targets=new();
        public GameObject markingEffect;
        
        private AtkTagSet atkTagSet = new AtkTagSet().Add(AtkTags.skillDamage, AtkTags.physicalDamage);

        public void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageTakeEvent d)
            {
                if(!this.targets.ContainsKey(d.target)) return;
                if (!d.atkTags.Contains(AtkTags.normalAttackDamage)) return;
                var m=this.targets[d.target];
                m.requireHit -= 1;
                if (m.requireHit <= 0)
                {
                    Destroy(m.effect);
                    d.target.removeListener(this);
                    this.targets.Remove(d.target);
                    
                    var stat = player.stat.calculate();
                    var tag = new AtkTagSet(atkTagSet);
                    var dmg = stat.calculateTrueDamage(tag, 600);
                    new DamageGiveEvent(dmg, Vector3.zero, player, d.target, tag, 15).trigger();
                    
                }
            }
        }

        public void registerTarget(Entity target, object args = null)
        {
            //Debug.Log(target);
            if (!targets.ContainsKey(target))
            {
                int reqHit = (int)((this.player.statCache.bulletRate*this.player.statCache.fireSpeed+2)*3);
                var effect = Instantiate(markingEffect);
                effect.transform.position = target.transform.position;
                var vector3 = effect.transform.position;
                vector3.z = -1;
                effect.transform.position = vector3;
                effect.transform.SetParent(target.transform);
                targets.Add(target, new Marking(duration, reqHit, effect));
                target.registerListener(this);
            }
            else
            {
                targets[target].time += this.duration;
            }
        }

        public void removeSelf()
        {
            
        }

        public void update(float deltaTime, Entity target)
        {
            if (this.targets.ContainsKey(target))
            {
                var m = this.targets[target];
                m.time -= deltaTime;
                if (m.time <= 0)
                {
                    this.targets.Remove(target);
                    Destroy(m.effect);
                    target.removeListener(this);
                }
            }
        }
    }
}