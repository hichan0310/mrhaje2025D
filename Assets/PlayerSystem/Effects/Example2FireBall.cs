using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Effects
{
    // 대충 뭔가 날아가서 피해를 주는게 있다고 합시다
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