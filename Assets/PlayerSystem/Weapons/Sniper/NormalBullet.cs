using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;

namespace PlayerSystem.Weapons.Sniper
{
    public class NormalBullet:SkillEffect
    {
        [SerializeField] private float speed;
        public DamageGiveEvent damageGiveEvent { get; set; }
        public Rigidbody2D rigidbody2D { get; set; }
        public GameObject hitEffect;
        private HaveTrailDestroy trailDestroy;

        public Vector2 direction
        {
            set => this.rigidbody2D.linearVelocity = value*speed;
        }

        private void Awake()
        {
            this.rigidbody2D = this.GetComponent<Rigidbody2D>();
            this.trailDestroy = this.GetComponent<HaveTrailDestroy>();
        }

        protected override void update(float deltaTime)
        {
            this.timer+=deltaTime;
            checkDestroy(10);
        }

        private void Start()
        {
            // Debug.Log(this.damageGiveEvent.trueDmg);
        }

        private bool finish = false;

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if(finish) return;
            var e = other.gameObject.GetComponent<Entity>();
            if(e!=null) if(e is Player) return;
            trailDestroy.destroy();
            this.rigidbody2D.linearVelocity = Vector2.zero;
            Destroy(Instantiate(hitEffect, this.transform.position, Quaternion.identity).gameObject, 1);
            damageGiveEvent.target = e;
            damageGiveEvent.trigger();
            this.finish = true;
        }
    }
}