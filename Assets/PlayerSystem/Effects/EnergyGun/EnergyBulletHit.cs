using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Skills;
using PlayerSystem.UltimateSkills.DamageEndingSniper;
using UnityEngine;

namespace PlayerSystem.Effects.EnergyGun
{
    public class EnergyBulletHit:SkillEffect
    {
        private Collider2D collider2D;
        private HashSet<Entity> targets;
        private AtkTagSet tags=new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.triggerEffectDamage);
        public IStat stat { get; set; }
        public Dictionary<Entity, DisplayStack> stack { get; set; }
        private float timer = 0;
        public float coef { get; set; }

        private void Start()
        {
            targets = new HashSet<Entity>();
            targets.Add(this.stat.entity);
            collider2D = GetComponent<Collider2D>();
            Destroy(gameObject,1f);
        }

        protected override void update(float deltaTime)
        {
            timer+=deltaTime;
            if(timer > 0.1f) collider2D.enabled = false;
            if(timer > 1f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var target = other.GetComponent<Entity>();
            if (target == null) return;
            if (targets.Contains(target)) return;
            targets.Add(target);
            var tag = new AtkTagSet(tags);
            var dmg=stat.calculateTrueDamage(tag, coef);
            new DamageGiveEvent(dmg, Vector3.zero, stat.entity, target, tag).trigger();
        }
    }
}