using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Skills.ElectricShock
{
    public class ElectricBombExplosion:SkillEffect
    {
        private Collider2D collider2D;
        private HashSet<Entity> targets;
        public BuffOnce shock { get; set; }
        public IStat stat{get;set;} 
        private AtkTagSet tags=new AtkTagSet().Add(AtkTags.electricalDamage, AtkTags.skillDamage);

        private void Start()
        {
            targets = new HashSet<Entity>();
            targets.Add(this.stat.entity);
            collider2D = GetComponent<Collider2D>();
        }

        protected override void update(float deltaTime)
        {
            timer += deltaTime;
            checkDestroy(1);
            if(timer>0.2f) collider2D.enabled = false;
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            var target = other.GetComponent<Entity>();
            if (target == null) return;
            if (targets.Contains(target)) return;
            targets.Add(target);

            var tag = new AtkTagSet(tags);
            var dmg=stat.calculateTrueDamage(tag, 200);
            new DamageGiveEvent(dmg, Vector3.zero, stat.entity, target, tag).trigger();
            shock.registerTarget(target);
        }
    }
}