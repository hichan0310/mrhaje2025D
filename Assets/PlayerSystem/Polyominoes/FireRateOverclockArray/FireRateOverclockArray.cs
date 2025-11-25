using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes
{
    public class FireRateOverclockArray : Polyomino
    {
        public override string Name => "FireRate Overclock Array";

        public override string Description =>
            "발사 속도가 5% 증가한다.\n" +
            "지속시간은 1초이며, 발동할 때마다 발사 속도 증가 스택이 1씩 증가하고 지속시간이 1초로 초기화된다.\n" +
            "이 효과는 무한히 중첩될 수 있다.";

        private static FireRateOverclockBuff buff;

        protected override void Start()
        {
            base.Start();
            if (buff == null) buff = new FireRateOverclockBuff();
        }

        public override void trigger(Entity entity, float power)
        {
            buff.registerTarget(entity);
        }

        private class FireRateOverclockBuff : IBuff, IEntityEventListener
        {
            public bool isStable => false;

            private Entity target;
            private int stack;
            private float time;
            private const float Duration = 1f;

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

                    stack = 0;
                    time = 0f;
                }

                if (stack < int.MaxValue)
                {
                    stack++;
                }

                time = Duration*(target is Player p?p.statCache.additionalDuration:1);
            }

            public void removeSelf()
            {
                if (target == null) return;

                target.removeListener(this);
                target.stat.removeBuff(this);
                target = null;
                stack = 0;
                time = 0f;
            }

            public void update(float deltaTime, Entity entity)
            {
                if (entity != target) return;
                if (stack <= 0) return;

                time -= deltaTime;
                if (time <= 0f)
                {
                    stack = 0;
                    target.removeListener(this);
                    target.stat.removeBuff(this);
                    target = null;
                    time = 0f;
                }
            }

            public void applyBuff(IStat stat)
            {
                if (stack <= 0) return;
                if (stat is not EntityStat entityStat) return;

                float factor = 1f + 0.05f * stack;
                entityStat.fireSpeed *= factor;
            }

            public void eventActive(EventArgs eventArgs)
            {
            }
        }
    }
}
