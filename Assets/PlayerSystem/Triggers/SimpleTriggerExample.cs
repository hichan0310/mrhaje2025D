﻿using EntitySystem;
using EntitySystem.Events;

namespace PlayerSystem.Triggers
{
    // 간단히 내부쿨 0.5초로 피해를 주면 파워 1f의 트리거를 전체에 발동하게 만들어봄
    public class SimpleTriggerExample : Trigger
    {
        private float timer;
        public Entity target;


        public override void eventActive(EventArgs eventArgs)
        {
            if (timer <= 0 || eventArgs is DamageGiveEvent)
            {
                timer = 0.5f;
                foreach (var effect in effects) effect.trigger(target, 1f);
            }
        }

        public override void registerTarget(Entity target, object args = null)
        {
            this.target = target;
        }

        public override void removeSelf()
        {
        }

        public override void update(float deltaTime, Entity target)
        {
            if (this.target != target)
            {
                timer -= deltaTime;
                if (timer < 0) timer = 0;
            }
        }
    }
}