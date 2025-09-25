using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.StatSystem;

namespace PlayerSystem.Effects
{
    // 트리거를 받으면 20% 3초동안 공격력 증가(중첩 불가)
    // BuffOnce에서 시간만 저장하고 다른 변수는 저장을 안 하게 만들어놔서 power 값을 쓰지를 못했다. 
    // 사실 여기 BuffOnce를 쓰는것보다 IBuff로 직접 처리하는게 더 효율적이지만 BuffType 쓰는 예시 정도는 된다. 
    public class SimpleEffectExample1:BuffOnce, ITriggerEffect
    {
        public void trigger(Entity entity, float power)
        {
            this.registerTarget(entity);
            this.targets[entity] = 3f;
        }

        public override bool isStable => true;

        // stable한 버프이기 때문에 register, remove 과정에서 stat에 직접 접근
        public override void applyBuff(IStat status){}

        public override void registerTarget(Entity target, object args = null)
        {
            if(targets.ContainsKey(target)) return;
            
            this.targets[target] = 2;
            target.registerListener(this);
            target.stat.registerBuff(this);
            target.stat.increaseAtk += 20;
        }

        public override void removeSelf()
        {
            foreach (var target in targets)
            {
                target.Key.removeListener(this);
                target.Key.stat.removeBuff(this);
                target.Key.stat.increaseAtk -= 20;
            }
            targets.Clear();
        }

        protected override void removeTarget(Entity target)
        {
            base.removeTarget(target);
            target.stat.increaseAtk -= 20;
        }
    }
}