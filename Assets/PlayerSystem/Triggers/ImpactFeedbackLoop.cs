using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ImpactFeedbackLoop:Board
    {
        public override string Name => "ImpactFeedbackLoop";
        public override string Description => "적에게 피해를 가하면 1초 내부쿨로 power=1의 트리거를 발동한다. \n" +
                                              "발사 속도에 반비례하여 내부쿨이 줄어든다. ";

        
        private float timer;
        
        public override void eventActive(EventArgs eventArgs)
        {
            if (timer <= 0)
            {
                this.trigger(1);
                timer = 1;
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            if (target == this.entity)
            {
                timer -= deltaTime;
                if (timer <= 0)
                {
                    timer = 0;
                }
            }
        }
    }
}