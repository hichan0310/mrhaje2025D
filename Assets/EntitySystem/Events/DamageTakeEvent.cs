using System.Collections.Generic;

namespace EntitySystem.Events
{
    public class DamageTakeEvent : EventArgs
    {
        public int realDmg { get; set; }
        public Entity attacker { get; }
        public Entity target { get; }
        public AtkTagSet atkTags { get; }


        public DamageTakeEvent(int realDmg, Entity attacker, Entity target, AtkTagSet atkTags)
        {
            name = "DmgTakeEvent";
            this.realDmg = realDmg;
            this.attacker = attacker;
            this.target = target;
            this.atkTags = atkTags;
        }

        public override void trigger()
        {
            UnityEngine.Debug.Log($"[DmgTake.trigger] atkTags? {(atkTags == null ? "null" : "ok")}, " +
                      $"Manager.Instance? {(DamageEventManager.Instance == null ? "null" : "ok")}, " +
                      $"target? {(target == null ? "null" : "ok")}");
            if (atkTags != null && atkTags.Contains(AtkTags.notTakeEvent)) return;

            DamageEventManager.Instance?.TriggerDmgTakeEvent(this);
            target?.eventActive(this);

            if (target != null && target.stat != null && target.stat.nowHp <= 0)
            {
                new EntityDieEvent(target, attacker).trigger();
            }
        }
    }
}