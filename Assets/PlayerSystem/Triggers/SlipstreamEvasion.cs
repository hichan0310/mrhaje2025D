using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class SlipstreamEvasion : Board
    {
        public override string Name => "슬립스트림 이베이전";
        public override string Description => "회피에 성공하면 power=1.2의 트리거를 발동한다. ";
        
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