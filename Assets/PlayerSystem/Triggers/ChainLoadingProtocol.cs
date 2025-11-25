using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ChainLoadingProtocol:Board
    {
        public override string Name => "체인로딩 프로토콜";
        public override string Description => "일반 공격을 발동하면 charge 수치가 1 증가한다. \n" +
                                              "이후 내부쿨 1초로 스킬이 명중하면 power=charge*0.2+0.5의 트리거를 발동한다. \n" +
                                              "발사 속도에 반비례하여 내부쿨이 줄어든다. ";
        
        private int charge = 0;
        private float timer = 0;
        
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is BasicAttackExecuteEvent)
            {
                charge += 1;
            }
            else if (eventArgs is DamageGiveEvent damageGiveEvent)
            {
                if (damageGiveEvent.atkTags.Contains(AtkTags.skillDamage))
                {
                    this.trigger(0.2f*this.charge+0.5f);
                    timer = 1;
                    if (this.entity is Player player)
                    {
                        timer = 2f / (player.statCache.fireSpeed + 1);
                    }
                }
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