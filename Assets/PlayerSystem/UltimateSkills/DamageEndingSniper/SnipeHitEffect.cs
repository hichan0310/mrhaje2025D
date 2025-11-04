using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.UltimateSkills.DamageEndingSniper
{
    public class SnipeHitEffect:MonoBehaviour
    {
        private Collider2D collider2D;
        private HashSet<Entity> targets;
        private AtkTagSet tags=new AtkTagSet().Add(AtkTags.heatDamage, AtkTags.ultimateDamage, AtkTags.criticalHit);
        public IStat stat { get; set; }
        public Dictionary<Entity, DisplayStack> stack { get; set; }
        private float timer = 0;

        private void Start()
        {
            targets = new HashSet<Entity>();
            targets.Add(this.stat.entity);
            collider2D = GetComponent<Collider2D>();
            Destroy(gameObject,1f);
        }

        private void Update()
        {
            timer+=Time.deltaTime;
            if(timer > 0.1f) collider2D.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var target = other.GetComponent<Entity>();
            if (target == null) return;
            if (targets.Contains(target)) return;
            targets.Add(target);
            float coef = 200;
            if (stack.ContainsKey(target))
            {
                coef+=2.5f*stack[target].stack;
            }

            var tag = new AtkTagSet(tags);
            var dmg=stat.calculateTrueDamage(tag, coef);
            new DamageGiveEvent(dmg, Vector3.zero, stat.entity, target, tag).trigger();
        }
    }
}