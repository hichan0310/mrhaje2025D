using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.DefenceBarrierMatrix
{
    public class DefenseBarrierMatrix : Polyomino
    {
        public override string Name => "Defense Barrier Matrix";

        public override string Description =>
            "방어력이 (30+30*power)% 증가한다.\n" +
            "지속시간은 10초이며, 증가량에는 최대치가 존재하지 않는다.\n" +
            "이 효과는 중첩되지 않으며, 여러 번 발동 시 가장 높은 증가량만 적용된다.";

        private static DefPriorityStack buff;

        protected override void Start()
        {
            base.Start();
            if (buff == null) buff = new DefPriorityStack();
        }

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

        private class DefPriorityStack : IBuff, IEntityEventListener
        {
            public bool isStable => false;

            private class Entry
            {
                public float bonus;
                public float time;
            }

            private Entity target;
            private readonly List<Entry> entries = new List<Entry>();
            private const float Duration = 10f;

            public void registerTarget(Entity target, object args = null)
            {
                if (args is not PowerSender p) return;

                if (this.target != target)
                {
                    if (this.target != null)
                    {
                        this.target.removeListener(this);
                        this.target.stat.removeBuff(this);
                        entries.Clear();
                    }

                    this.target = target;
                    if (target == null) return;

                    target.registerListener(this);
                    target.stat.registerBuff(this);
                }

                float bonus = 30f + 30f * p.power;

                entries.Add(new Entry
                {
                    bonus = bonus,
                    time = Duration*(target is Player pl?pl.statCache.additionalDuration:1)
                });
            }

            public void removeSelf()
            {
                if (target == null) return;

                target.removeListener(this);
                target.stat.removeBuff(this);
                target = null;
                entries.Clear();
            }

            public void update(float deltaTime, Entity entity)
            {
                if (entity != target) return;
                if (entries.Count == 0) return;

                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    entries[i].time -= deltaTime;
                    if (entries[i].time <= 0f)
                    {
                        entries.RemoveAt(i);
                    }
                }

                if (entries.Count == 0)
                {
                    target.removeListener(this);
                    target.stat.removeBuff(this);
                    target = null;
                }
            }

            public void applyBuff(IStat stat)
            {
                if (stat is not EntityStat entityStat) return;
                if (entries.Count == 0) return;

                float max = 0f;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].bonus > max)
                    {
                        max = entries[i].bonus;
                    }
                }

                if (max > 0f)
                {
                    entityStat.increaseDef += max;
                }
            }

            public void eventActive(EventArgs eventArgs)
            {
            }
        }
    }
}
