using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Polyominoes.FireKnife
{
    public class Burning:DamageOverTime
    {
        protected override float cooldown => 2;
        public float duration { get; set; }

        public override void registerTarget(Entity target, object args = null)
        {
            Debug.Log("Burning");
            base.registerTarget(target, args);
            this.transform.position=target.transform.position;
            this.transform.SetParent(target.transform);
        }

        public override void update(float deltaTime, Entity target)
        {
            base.update(deltaTime, target);
            duration -= deltaTime;
            if (duration < 0)
            {
                removeSelf();
                Destroy(this.gameObject);
            }
        }
    }
}