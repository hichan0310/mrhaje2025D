using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.NextShotAmplifier
{
    public class NextShotAmplifier : Polyomino
    {
        public override string Name => "Next Shot Amplifier";

        public override string Description =>
            "다음 일반 공격의 피해가 (10+10*power)% 증폭된다.\n" +
            "이 효과는 최대 3번까지 중첩되며, 항상 power 값이 높은 3개의 스택만 유지된다. 누적 피해 증폭은 최대 150%까지 적용된다.\n" +
            "지속시간은 무한이며, 일반 공격으로 피해를 입히는 순간 모든 스택이 소모된다.";

        private static NextBasicAttackDamageBuff buff = new NextBasicAttackDamageBuff();

        public override void trigger(Entity entity, float power)
        {
            buff.registerTarget(entity, new PowerSender(power));
        }

        private class PowerSender
        {
            public float power { get; }

            public PowerSender(float power)
            {
                this.power = power;
            }
        }

        private class NextBasicAttackDamageBuff : IBuff, IEntityEventListener
        {
            public bool isStable => true;

            // attacker 기준으로 스택 관리
            private readonly Dictionary<Entity, List<float>> stacks = new();
            private const int MaxStacks = 3;

            public void eventActive(EventArgs eventArgs)
            {
                if (eventArgs is not DamageGiveEvent dmg) return;
                var attacker = dmg.attacker;
                if (attacker == null) return;
                if (!stacks.TryGetValue(attacker, out var list)) return;
                if (list.Count == 0) return;
                if (!dmg.atkTags.Contains(AtkTags.normalAttackDamage)) return;

                float totalPercent = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    float v = 10 + 10 * list[i];
                    if (v < 0) v = 0;
                    totalPercent += v;
                }

                if (totalPercent > 150) totalPercent = 150;

                dmg.trueDmg = (int)(dmg.trueDmg * (1 + totalPercent / 100f));

                removeTarget(attacker);
            }

            public void registerTarget(Entity target, object args = null)
            {
                if (target == null) return;
                if (args is not PowerSender p) return;

                if (!stacks.ContainsKey(target))
                {
                    stacks[target] = new List<float>();
                    target.registerListener(this);
                    target.stat.registerBuff(this);
                }

                var list = stacks[target];
                list.Add(p.power);
                list.Sort((a, b) => b.CompareTo(a));
                if (list.Count > MaxStacks)
                {
                    list.RemoveRange(MaxStacks, list.Count - MaxStacks);
                }
            }

            public void removeSelf()
            {
                foreach (var pair in stacks)
                {
                    var entity = pair.Key;
                    entity.removeListener(this);
                    entity.stat.removeBuff(this);
                }

                stacks.Clear();
            }

            private void removeTarget(Entity target)
            {
                if (!stacks.ContainsKey(target)) return;
                stacks.Remove(target);
                target.removeListener(this);
                target.stat.removeBuff(this);
            }

            public void update(float deltaTime, Entity target)
            {
            }

            public void applyBuff(IStat stat)
            {
            }
        }
    }
}