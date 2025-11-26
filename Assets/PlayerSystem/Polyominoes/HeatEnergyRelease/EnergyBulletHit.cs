using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;

namespace PlayerSystem.Polyominoes.HeatEnergyRelease
{
    public class EnergyBulletHit : SkillEffect
    {
        private Collider2D collider2D;
        private HashSet<Entity> targets;
        private float timer = 0;
        public DamageGiveEvent damageGiveEvent { get; set; }

        private void Start()
        {
            targets = new HashSet<Entity>();
            targets.Add(this.damageGiveEvent.attacker);
            collider2D = GetComponent<Collider2D>();
            Destroy(gameObject, 1f);
        }

        protected override void update(float deltaTime)
        {
            timer += deltaTime;
            if (timer > 0.1f) collider2D.enabled = false;
            if (timer > 1f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var target = other.GetComponent<Entity>();
            if (target == null) return;
            if (targets.Contains(target)) return;
            targets.Add(target);
            damageGiveEvent.target = target;
            damageGiveEvent.trigger();
            damageGiveEvent.energeRecharge = 0;
        }
    }
}