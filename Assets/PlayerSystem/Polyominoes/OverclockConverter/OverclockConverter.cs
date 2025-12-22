using EntitySystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.OverclockConverter
{
    public class OverclockConverter: Polyomino
    {
        public override string Name => "OverclockConverter";
        public override string Description => 
            "현재 체력의 5%를 소모하고 자기 자신의 방어력을 20% 감소시키면서 5초동안 가하는 모든 피해를 power*15% 증가시킨다. \n" +
            "이 효과는 중첩되지 않는다. ";
        public override void trigger(Entity player, float power)
        {
            
        }
    }
}