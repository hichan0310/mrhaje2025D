using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Triggers
{
    public class RebootProtocol : Board
    {
        public override string Name => "리부트 프로토콜";
        public override string Description => "가한 누적 피해량이 최대체력의 5배 이상이 되면 효과가 활성화된다. \n" +
                                              "이 상태에서 죽음에 해당하는 피해를 받으면 hp 1로 생존하고, power=9의 트리거를 발동한다. ";
        
        private int damageCharge = 0;

        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent damageGiveEvent)
            {
                if (damageGiveEvent.attacker == this.entity)
                    this.damageCharge += damageGiveEvent.trueDmg;
            }
            else if (eventArgs is DamageTakeEvent damageTakeEvent)
            {
                if(damageTakeEvent.target!=this.entity) return;
                if (this.entity is Player p)
                {
                    var stat=p.statCache;
                    if (this.damageCharge<stat.maxHp*5+stat.atk*50+stat.def*50) return;
                    if (this.entity.stat.nowHp <= 0) this.entity.stat.nowHp = 1;
                    this.trigger(9);
                }
            }
        }

        public override void update(float deltaTime, Entity target)
        {
        }
    }
}