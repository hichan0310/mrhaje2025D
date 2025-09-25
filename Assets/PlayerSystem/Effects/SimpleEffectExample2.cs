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
            // 위치, 타겟 방향, 개수 등은 귀찮으니 나중에 설정하자

            // 다 만들고 발견했는데 calculateTrueDamage() 부분에서 tag에 AtkTags.criticalHit가 들어갈 수도 있으니
            // calculate() 돌린 EntityStat을 넘기는게 나을듯
            var stat = entity.stat.calculate();
            var tag = new AtkTagSet(atkTagSet);
            var dmg=stat.calculateTrueDamage(tag, 100*power);
            f.damage = new(dmg, Vector3.zero, entity, null, tag);
        }
    }
}