using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ModuleBoostCall:Board
    {
        public override string Name => "모듈 부스트 콜";
        public override string Description => "스킬을 사용하면 power=2의 트리거를 발동한다. ";

        
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