using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class SlipstreamEvasion : Board
    {
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DodgeEvent)
            {
                this.trigger(1.2f);
            }
        }

        public override void update(float deltaTime, Entity target)
        {
        }
    }
}