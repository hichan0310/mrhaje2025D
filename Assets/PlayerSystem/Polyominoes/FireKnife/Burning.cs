using EntitySystem;
using EntitySystem.BuffTypes;
using UnityEngine;

namespace PlayerSystem.Polyominoes.FireKnife
{
    public class Burning : DamageOverTime
    {
        protected override float cooldown => 2;
        public float duration { get; set; }

        public override void registerTarget(Entity target, object args = null)
        {
            if (!target) return;
            base.registerTarget(target);
            transform.SetParent(target.transform, false);
            transform.localPosition = Vector3.zero;
            var body = target.GetComponentInChildren<SpriteRenderer>();
            foreach (var renderer in GetComponentsInChildren<ParticleSystemRenderer>())
            {
                if (body) renderer.sortingLayerID = body.sortingLayerID;
                renderer.sortingOrder = body ? body.sortingOrder + 2 : 20;
            }
        }

        public override void update(float deltaTime, Entity entity)
        {
            if (entity != target || deltaTime <= 0) return;
            if (!target || target.stat == null || target.stat.nowHp <= 0) { Destroy(gameObject); return; }
            timer += Mathf.Min(deltaTime, Mathf.Max(0, duration));
            duration -= deltaTime;
            while (timer >= cooldown && target && target.stat.nowHp > 0)
            {
                timer -= cooldown;
                giveDamage();
            }
            if (duration <= 0) { removeSelf(); Destroy(gameObject); }
        }
    }
}
