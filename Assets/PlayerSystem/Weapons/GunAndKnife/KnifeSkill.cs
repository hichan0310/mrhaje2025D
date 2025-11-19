using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class KnifeSkill : SkillEffect
    {
        [SerializeField] private float speed;
        public DamageGiveEvent damageGiveEventHit { get; set; }
        public Rigidbody2D rigidbody2D { get; set; }
        private HaveTrailDestroy trailDestroy;
        public Mark marking { get; set; }

        public Vector2 direction
        {
            set => this.rigidbody2D.linearVelocity = value * speed;
        }

        private void Awake()
        {
            this.rigidbody2D = this.GetComponent<Rigidbody2D>();
            this.trailDestroy = this.GetComponent<HaveTrailDestroy>();
        }

        protected override void update(float deltaTime)
        {
            this.timer += deltaTime;
            checkDestroy(0.5f);
            if (finish) Destroy(this.gameObject);
        }

        private bool finish = false;

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (finish) return;
            var e = other.gameObject.GetComponent<Entity>();
            if (e == null) return;
            if (e is Player) return;
            // Debug.Log(other.gameObject.name);
            Destroy(gameObject);
            this.rigidbody2D.linearVelocity = Vector2.zero;
            damageGiveEventHit.target = e;
            damageGiveEventHit.trigger();

            marking.registerTarget(e);

            this.finish = true;
        }
    }
}