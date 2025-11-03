using System;
using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.StatSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlayerSystem.Skills.ElectricShock
{
    public class ElectricBomb:SkillEffect
    {
        public ElectricBombExplosion explosion;
        public BuffOnce shock{get;set;}
        public IStat stat{get;set;}
        public float angle{get;set;}
        public float velocity{get;set;}
        

        private void Start()
        {
            this.rigidbody2D.linearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * velocity;
        }

        protected override void update(float deltaTime)
        {
            timer+=deltaTime;
            if(timer >= 10f) explode();
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponent<SkillEffect>()) return;
            var p = other.gameObject.GetComponent<Entity>();
            if (p == null)
            {
                explode();
                return;
            }
            // Debug.Log(p);
            // Debug.Log(this.stat.entity);
            if (p == this.stat.entity)
            {
                // Debug.Log(other.gameObject.name);
                return;
            }
            explode();
        }

        private void explode()
        {
            var explode = Instantiate(explosion);
            explode.transform.position = this.transform.position;
            explode.stat = this.stat;
            explode.shock = this.shock;
            Destroy(gameObject);
        }
    }
}