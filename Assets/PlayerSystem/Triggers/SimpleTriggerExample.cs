using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Triggers
{
    // 간단히 내부쿨 0.5초로 피해를 주면 파워 1f의 트리거를 전체에 발동하게 만들어봄
    public class SimpleTriggerExample : Board
    {
        private float timer;
        

        private void Awake()
        {
            
        }


        public override void eventActive(EventArgs eventArgs)
        {
            if (timer <= 0 && eventArgs is DamageGiveEvent)
            {
                timer = 0.5f;
                foreach (var effect in this.polyominos) effect.p.trigger(this.entity, 1f);
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            if (this.entity == target)
            {
                timer -= deltaTime;
                if (timer < 0) timer = 0;
            }
        }

        public override string Name => "temp";
        public override string Description => "temp";
    }
}