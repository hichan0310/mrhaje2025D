using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class OverchargeSurge:Board
    {
        public override string Name => "OverchargeSurge";
        public override string Description => "저스트 회피에 성공하면 회피한 공격의 피해량/300만큼 charge가 증가한다. \n" +
                                              "이후 필살기를 발동하면 power=charge+3의 트리거를 추가로 발동한다. \n" +
                                              "charge 최대치는 6이다. ";
        
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