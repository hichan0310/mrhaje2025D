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
        
        public HashSet<Entity> colliders = new HashSet<Entity>();
        private Collider2D collider2D;
        private float timer = 0;

        private void Start()
        {
            collider2D = GetComponent<Collider2D>();
            Destroy(this.gameObject, 1);
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
            var e = other.GetComponentInParent<Entity>();
            if (e == null || colliders.Contains(e)) return;
            colliders.Add(e);
            if (e == null) return;
            if (e == this.damageGiveEvent.attacker) return;
            this.damageGiveEvent.target = e;
            this.damageGiveEvent.trigger();
        }
    }
}