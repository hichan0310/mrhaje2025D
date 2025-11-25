using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ReverseHackReflection:Board
    {
        public override string Name => "리버스해킹 리플렉션";
        public override string Description => "저스트 회피에 성공하면 power=2+회피한 공격의 피해량/200의 트리거를 발동한다. ";
        
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