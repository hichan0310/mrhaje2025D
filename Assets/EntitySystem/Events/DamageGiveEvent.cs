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
        

        public DamageGiveEvent(int trueDmg, Vector3 force, Entity attacker, Entity target, AtkTagSet atkTags)
        {
            name="DmgGiveEvent";
            this.trueDmg = trueDmg;
            this.force = force;
            this.attacker = attacker;
            this.target = target;
            this.atkTags = atkTags ?? AtkTagSet.None;
        }

        public override void trigger()
        {
            if (target)
            {
                if (attacker)
                    attacker.eventActive(this);
                if (target is Player player)
                {
                    if (player.TryInterceptAttack(attacker, this))
                    {
                        new JustDodgeEvent(player, this).trigger();
                        return;
                    }
                }
                target.takeDamage(this);
            }
        }
    }
}