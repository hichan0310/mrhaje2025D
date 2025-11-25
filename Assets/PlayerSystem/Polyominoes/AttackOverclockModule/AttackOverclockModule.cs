using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.AttackOverclockModule
{
    public class AttackOverclockModule : Polyomino
    {
        public override string Name => "Attack Overclock Module";

        public override string Description =>
            "공격력이 (10+10*power)% 증가한다.\n" +
            "지속시간은 10초이며, 이 효과로 인한 공격력 증가는 최대 100%까지 적용된다.\n" +
            "이 효과는 중첩되지 않는다.";

        private static AtkUp atkUp;

        protected override void Start()
        {
            base.Start();
            if (atkUp == null) atkUp = new AtkUp();
        }

        public override void trigger(Entity entity, float power)
        {
            atkUp.registerTarget(entity, new PowerSender(power));
        }


        private class PowerSender
        {
            public float power { get; set; }

            public PowerSender(float power)
            {
                this.power = power;
            }
        }

        private class AtkUp : IBuff, IEntityEventListener
        {
            public bool isStable => true;
            public Entity target;
            public float power;
            private float time;
            private const float Duration = 10;

            public void registerTarget(Entity target, object args = null)
            {
                if (this.target == target)
                {
                    this.time = Duration * (target is Player p ? p.statCache.additionalDuration : 1);
                }
                else if (args is PowerSender p)
                {
                    this.target.stat.increaseAtk -= 10 + 10 * this.power;
                    target.removeListener(this);
                    target.stat.removeBuff(this);

                    target.registerListener(this);
                    target.stat.registerBuff(this);
                    this.power = p.power;
                    this.target = target;
                    this.target.stat.increaseAtk += 10 + 10 * this.power;
                    this.time = Duration * (target is Player pl ? pl.statCache.additionalDuration : 1);
                }
            }

            public void removeSelf()
            {
                target.removeListener(this);
                target.stat.removeBuff(this);
            }

            public void update(float deltaTime, Entity target)
            {
                time -= deltaTime;
                if (time <= 0)
                {
                    target.removeListener(this);
                    target.stat.removeBuff(this);
                }
            }

            public void applyBuff(IStat stat)
            {
            }

            public void eventActive(EventArgs eventArgs)
            {
            }
        }
    }
}