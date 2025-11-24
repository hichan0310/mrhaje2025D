using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class RapidFirePulse:Board
    {
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is BasicAttackExecuteEvent)
            {
                if (this.entity is Player player)
                {
                    this.trigger(player.statCache.bulletRate/10);
                }
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            
        }
    }
}