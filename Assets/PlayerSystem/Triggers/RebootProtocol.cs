using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Triggers
{
    public class RebootProtocol : Board
    {
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
                if (this.entity.stat.nowHp <= 0) this.entity.stat.nowHp = 1;
                this.trigger(9);
            }
        }

        public override void update(float deltaTime, Entity target)
        {
        }
    }
}