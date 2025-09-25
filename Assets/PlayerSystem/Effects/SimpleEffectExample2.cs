using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Effects
{
    public class SimpleEffectExample2 : MonoBehaviour, ITriggerEffect
    {
        public Example2FireBall fireball;
        private AtkTagSet atkTagSet = new AtkTagSet().Add(AtkTags.fireball);

        public void trigger(Entity entity, float power)
        {
            var f=Instantiate(fireball);
            // 위치, 타겟 방향 등은 귀찮으니 나중에 설정하자


            var stat = entity.stat.calculate();
            var tag = new AtkTagSet(atkTagSet);
            var dmg=stat.calculateTrueDamage(tag, 100*power);
            f.damage = new(dmg, Vector3.zero, entity, null, tag);
        }
    }
}