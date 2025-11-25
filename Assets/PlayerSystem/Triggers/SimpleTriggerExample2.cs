using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Triggers
{
    public class SimpleTriggerExample2:Board
    {
        private float timer = 0;
        
        public override void eventActive(EventArgs eventArgs)
        {
            
        }

        public override void update(float deltaTime, Entity target)
        {
            if (target == this.entity)
            {
                timer -= deltaTime;
                if (timer <= 0)
                {
                    this.trigger(1);
                    timer += 1;
                }
            }
        }

        public override string Name => "temp";
        public override string Description => "1초마다 1짜리";
    }
}