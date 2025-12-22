using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.UltimateCapacitor
{
    public class UltimateCapacitor : Polyomino, IEntityEventListener, IBuff
    {
        public override string Name => "UltimateCapacitor";

        public override string Description =>
            "(power-1)*1.3의 수치만큼 파워를 최대 20까지 자체적으로 축적한다. \n" +
            "만약 1보다 작은 파워가 들어오면 누적되지 않는다. \n" +
            "이후 필살기를 발동하면 3초동안 필살기 피해가 축적한 파워*20%만큼 증가한다. \n" +
            this.statBonusDescription;

        private Entity player;
        private float power = 0;

        private float buffTimer = 0;

        public override void trigger(Entity player, float power)
        {
            if (!this.player || this.player != player)
            {
                registerTarget(player);
            }

            this.power += (power - 1) * 1.3f;
            if (power >= 20) power = 20;
        }

        public void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is UltimateExecuteEvent)
            {
                buffTimer = 3;
            }
        }

        public void registerTarget(Entity target, object args = null)
        {
            removeSelf();
            this.player = target;
            target.registerListener(this);
            target.stat.registerBuff(this);
        }

        public bool isStable { get; }

        public void applyBuff(IStat stat)
        {
            if (this.buffTimer > 0)
            {
                if (stat is EntityStat entityStat)
                {
                    entityStat.dmgUp[(int)AtkTags.ultimateDamage] += power * 20;
                }
            }
        }

        public void removeSelf()
        {
            if (this.player != null)
            {
                this.player.removeListener(this);
                this.player.stat.removeBuff(this);
            }
        }

        public void update(float deltaTime, Entity target)
        {
            if (buffTimer > 0)
            {
                buffTimer -= deltaTime;
                if (buffTimer <= 0)
                {
                    buffTimer = 0;
                    this.power = 0;
                    this.player = null;
                }
            }
        }
    }
}