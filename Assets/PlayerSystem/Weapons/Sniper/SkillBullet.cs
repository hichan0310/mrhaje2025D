using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;

namespace PlayerSystem.Weapons.Sniper
{
    public class SkillBullet:SkillEffect
    {
        [SerializeField] private float speed;
        public DamageGiveEvent damageGiveEvent { get; set; }
        public Rigidbody2D rigidbody2D { get; set; }
        public SkillHit hitEffect;
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
            //Debug.Log(this.damageGiveEvent.trueDmg);
        }

        private bool finish = false;

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if(finish) return;
            var e = other.gameObject.GetComponent<Entity>();
            if(e==null) return;
            if(e is Player) return;
            Debug.Log(other.gameObject.name);
            trailDestroy.destroy();
            this.rigidbody2D.linearVelocity = Vector2.zero;
            var t = Instantiate(hitEffect, this.transform.position, Quaternion.identity);
            t.damageGiveEvent = this.damageGiveEvent;
            this.finish = true;
        }
    }
}