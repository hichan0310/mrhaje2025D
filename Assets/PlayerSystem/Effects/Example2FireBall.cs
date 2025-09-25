using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Effects
{
    public class Example2FireBall:MonoBehaviour
    {
        public DamageGiveEvent damage{get;set;}

        private void giveDamage(Entity entity)
        {
            damage.target = entity;
            damage.trigger();
        }
    }
}