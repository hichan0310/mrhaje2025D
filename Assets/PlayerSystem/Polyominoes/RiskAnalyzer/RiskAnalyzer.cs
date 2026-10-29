using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.RiskAnalyzer
{
    public class RiskAnalyzer: Polyomino, IEntityEventListener, IBuff
    {
        public override string Name => "RiskAnalyzer";
        
        private float lifeTime;
        private float power;

        private Entity target;
        
        public override string Description => 
            "10초동안 방어력을 (power+1)*15%만큼 상승시킨다. \n" +
            this.statBonusDescription;
        
        public override void trigger(Entity player, float power)
        {
            if (player is Player p)
            {
                lifeTime = p.statCache.additionalDuration + 10;
                this.power = power;
                if(this.target)
                    this.removeSelf();
                registerTarget(player);
            }
        }

        public bool isStable => false;
        public void applyBuff(IStat stat)
        {
            if (stat is EntityStat entityStat)
            {
                entityStat.increaseDef += (power+1)*15;
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
            if(lifeTime < 0)
                removeSelf();
        }
    }
}
