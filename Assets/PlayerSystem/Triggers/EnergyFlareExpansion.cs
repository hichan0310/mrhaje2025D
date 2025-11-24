using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class EnergyFlareExpansion : Board
    {
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is UltimateExecuteEvent ultimateExecuteEvent)
            {
                this.trigger((float)ultimateExecuteEvent.energy / 10);
            }
        }

        public override void update(float deltaTime, Entity target)
        {
        }
    }
}