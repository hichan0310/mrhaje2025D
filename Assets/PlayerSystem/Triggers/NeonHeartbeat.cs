using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class NeonHeartbeat : Board
    {
        public override string Name => "NeonHeartbeat";
        public override string Description => "1초마다 자동으로 power=0.3의 트리거를 발동한다. ";
        
        private float timer;

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
                    this.trigger(0.3f);
                    timer += 1;
                }
            }
        }
        
    }
}