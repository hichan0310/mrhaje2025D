using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons.Sniper
{
    public class UltimateFinished:MonoBehaviour
    {
        public DamageGiveEvent damageGiveEvent { get; set; }
        
        public HashSet<Collider2D> colliders = new HashSet<Collider2D>();
        private Collider2D collider2D;
        private float timer = 0;

        private void Start()
        {
            collider2D = GetComponent<Collider2D>();
        }

        private void Update()
        {
            timer+=Time.deltaTime;
            if (timer >= 0.5)
            {
                collider2D.enabled = false;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (colliders.Contains(other)) return;
            colliders.Add(other);
            var e = other.GetComponent<Entity>();
            if (e == null) return;
            if (e == this.damageGiveEvent.attacker) return;
            this.damageGiveEvent.target = e;
            this.damageGiveEvent.trigger();
            this.damageGiveEvent.energeRecharge = 0;
        }
    }
}