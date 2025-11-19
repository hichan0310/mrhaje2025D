using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;

namespace PlayerSystem.Weapons.Sniper
{
    public class SkillHit:SkillEffect
    {
        private bool active = true;
        private Collider2D collider2D;
        private HashSet<Collider2D> colliders = new HashSet<Collider2D>();
        public DamageGiveEvent damageGiveEvent { get; set; }
        
        private void Start()
        {
            collider2D = GetComponent<Collider2D>();
        }

        protected override void update(float deltaTime)
        {
            if (!active) return;
            timer+=deltaTime;
            if (timer >= 0.1f)
            {
                active = false;
                this.collider2D.enabled = false;
            }
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if(colliders.Contains(other)) return;
            colliders.Add(other);
            var e=other.gameObject.GetComponent<Entity>();
            if(e == null) return;
            this.damageGiveEvent.target = e;
            this.damageGiveEvent.trigger();
        }
    }
}