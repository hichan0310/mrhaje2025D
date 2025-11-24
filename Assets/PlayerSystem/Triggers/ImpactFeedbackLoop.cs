using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ImpactFeedbackLoop:Board
    {
        private float timer;
        
        public override void eventActive(EventArgs eventArgs)
        {
            if (timer <= 0)
            {
                this.trigger(1);
                timer = 1;
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            if (target == this.entity)
            {
                timer -= deltaTime;
                if (timer <= 0)
                {
                    timer = 0;
                }
            }
        }
    }
}