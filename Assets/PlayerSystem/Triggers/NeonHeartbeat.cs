using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class NeonHeartbeat : Board
    {
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