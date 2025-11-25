using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class EnergyFlareExpansion : Board
    {
        public override string Name => "에너지 플레어 익스팬션";
        public override string Description => "필살기를 사용하면 power=사용한 에너지/10의 트리거를 발동한다. ";

        
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