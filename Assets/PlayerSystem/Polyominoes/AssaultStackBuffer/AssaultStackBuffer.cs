using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.AssaultStackBuffer
{
    public class AssaultStackBuffer : Polyomino
    {
        public override string Name => "Assault Stack Buffer";

        public override string Description =>
            "공격력이 10 증가한다.\n" +
            "이 효과는 power에 영향을 받지 않으며, 최대 +2000까지 중첩될 수 있다.\n" +
            "지속시간은 3초이고, 피해를 줄 때마다 지속시간이 3초로 초기화된다.";

        private static AssaultStackBuff buff;

        protected override void Start()
        {
            base.Start();
            if (buff == null) buff = new AssaultStackBuff();
        }

        public override void trigger(Entity entity, float power)
        {
            buff.registerTarget(entity);
        }

        private class AssaultStackBuff : IBuff, IEntityEventListener
        {
            public bool isStable => false;

            private Entity target;
            private float time;
            private float stack; // addAtk에 그대로 더해질 값
            private const float Duration = 3;

            public void registerTarget(Entity target, object args = null)
            {
                if (this.target != target)
                {
                    if (this.target != null)
                    {
                        this.target.removeListener(this);
                        this.target.stat.removeBuff(this);
                    }

                    this.target = target;
                    if (target == null) return;

                    target.registerListener(this);
                    target.stat.registerBuff(this);
                    stack = 0f;
                }

                if (stack < 2000f)
                {
                    stack += 10f;
                    if (stack > 2000f) stack = 2000f;
                }

                time = Duration * (target is Player pl ? pl.statCache.additionalDuration : 1);
            }

            public void removeSelf()
            {
                if (target == null) return;

                target.removeListener(this);
                target.stat.removeBuff(this);
                target = null;
                stack = 0f;
                time = 0f;
            }

            public void update(float deltaTime, Entity entity)
            {
                if (entity != target) return;

                time -= deltaTime;
                if (time <= 0f)
                {
                    target.removeListener(this);
                    target.stat.removeBuff(this);
                    target = null;
                    stack = 0f;
                    time = 0f;
                }
            }

            public void applyBuff(IStat stat)
            {
                if (stat is EntityStat entityStat)
                {
                    entityStat.addAtk += (int)stack;
                }
            }

            public void eventActive(EventArgs eventArgs)
            {
                if (target == null) return;

                if (eventArgs is DamageGiveEvent)
                {
                    time = Duration * (target is Player pl ? pl.statCache.additionalDuration : 1);
                }
            }
        }
    }
}