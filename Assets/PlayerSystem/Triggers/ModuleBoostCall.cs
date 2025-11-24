using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ModuleBoostCall:Board
    {
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is SkillExecuteEvent)
            {
                this.trigger(2);
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            
        }
    }
}