using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ReverseHackReflection:Board
    {
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is JustDodgeEvent justDodgeEvent)
            {
                this.trigger(2f+(float)justDodgeEvent.damageGiveEvent.trueDmg/200);
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            
        }
    }
}