using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.OverclockConverter
{
    public class OverclockConverter : Polyomino, IEntityEventListener, IBuff
    {
        public override string Name => "OverclockConverter";

        private float lifeTime;
        private float power;

        private Entity target;

        public override string Description =>
            "현재 체력의 5%를 소모하고 5초동안 자기 자신의 방어력을 20% 감소시키면서 가하는 모든 피해를 power*15% 증가시킨다. \n" +
            "이 효과는 중첩되지 않는다. ";

        public override void trigger(Entity player, float power)
        {
            if (player is Player p)
            {
                lifeTime = p.statCache.additionalDuration + 5;
                this.power = power;
                if (this.target)
                    this.removeSelf();
                registerTarget(player);
            }
        }

        public bool isStable => false;

        public void applyBuff(IStat stat)
        {
            if (stat is EntityStat entityStat)
            {
                entityStat.increaseDef -= 20;
                entityStat.dmgUp[(int)AtkTags.all] += power * 15;
            }
        }

        public void eventActive(EventArgs eventArgs)
        {
        }

        public void registerTarget(Entity target, object args = null)
        {
            this.target = target;
            this.target.registerListener(this);
            this.target.stat.registerBuff(this);
        }

        public void removeSelf()
        {
            this.target.removeListener(this);
            this.target.stat.removeBuff(this);
        }

        public void update(float deltaTime, Entity target)
        {
            lifeTime -= deltaTime;
            if (lifeTime < 0)
                removeSelf();
        }
    }
}