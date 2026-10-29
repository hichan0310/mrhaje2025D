using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

namespace EntitySystem.Events
{
    public class DamageGiveEvent:EventArgs
    {
        public int trueDmg { get; set; }
        public Entity attacker { get; }
        public Entity target { get; set; }
        public Vector3 force { get; set; }
        public AtkTagSet atkTags { get; set; }
        public float energeRecharge { get; set; }
        

        public DamageGiveEvent(int trueDmg, Vector3 force, Entity attacker, Entity target, AtkTagSet atkTags, float energeRecharge)
        {
            name="DmgGiveEvent";
            this.trueDmg = trueDmg;
            this.force = force;
            this.attacker = attacker;
            this.target = target;
            this.atkTags = atkTags ?? AtkTagSet.None;
            this.energeRecharge = energeRecharge;
        }

        public override void trigger()
        {
            if (!target || target.stat == null || target.stat.nowHp <= 0) return;
            // A missed/dodged attack must not grant the attacker on-hit buffs or energy.
            if (target is Player player && player.TryInterceptAttack(attacker, this))
            {
                new JustDodgeEvent(player, this).trigger();
                return;
            }
            if (attacker)
            {
                attacker.eventActive(this);
                this.energeRecharge = 0;
            }
            if (!target || target.stat == null) return;
            int healthBefore = target.stat.nowHp;
            target.takeDamage(this);
            int dealt = UnityEngine.Mathf.Max(0, healthBefore - target.stat.nowHp);
            if (dealt > 0) new DamageDealtEvent(attacker, target, dealt, atkTags).trigger();
        }
    }
}
