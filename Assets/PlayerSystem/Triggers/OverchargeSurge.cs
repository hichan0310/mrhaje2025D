using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class OverchargeSurge:Board
    {
        private float charge=0;
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is JustDodgeEvent justDodgeEvent)
            {
                charge+=(float)justDodgeEvent.damageGiveEvent.trueDmg/300;
                if (charge > 6) charge = 6;
            }
            else if (eventArgs is UltimateExecuteEvent)
            {
                this.trigger(3+charge);
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            
        }
    }
}