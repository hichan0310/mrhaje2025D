using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;

namespace PlayerSystem.Weapons.Sniper
{
    public class SkillHit : SkillEffect
    {
        private bool active = true;
        private CircleCollider2D collider2D;
        private HashSet<Collider2D> colliders = new HashSet<Collider2D>();
        public DamageGiveEvent damageGiveEvent { get; set; }
        private ParticleSystem[] particleSystems;

        private void Start()
        {
            collider2D = GetComponent<CircleCollider2D>();
            float range = 1;
            if (this.damageGiveEvent.attacker is Player p)
                range = p.statCache.skillRange;
            range = Mathf.Sqrt(range);
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            this.transform.localScale *= range;
            // collider2D.radius *= range;
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.startSizeMultiplier *= range;
            }
        }

        protected override void update(float deltaTime)
        {
            if (!active) return;
            timer += deltaTime;
            if (timer >= 0.1f)
            {
                active = false;
                this.collider2D.enabled = false;
            }
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (colliders.Contains(other)) return;
            colliders.Add(other);
            var e = other.gameObject.GetComponent<Entity>();
            if (e == null) return;
            // if (e == this.damageGiveEvent.attacker) return;
            this.damageGiveEvent.target = e;
            this.damageGiveEvent.trigger();
        }
    }
}