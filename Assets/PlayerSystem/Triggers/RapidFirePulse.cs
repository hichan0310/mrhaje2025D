using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class RapidFirePulse:Board
    {
        public override string Name => "RapidFirePulse";
        public override string Description => "일반 공격이 발동하면 power=총알 개수/10의 트리거를 발동한다. \n" +
                                              "내부쿨은 존재하지 않는다. ";
        
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