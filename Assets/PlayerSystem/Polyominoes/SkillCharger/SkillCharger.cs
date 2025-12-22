using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.SkillCharger
{
    public class SkillCharger: Polyomino, IEntityEventListener, IBuff
    {
        public override string Name => "SkillCharger";
        
        private float lifeTime;
        private float power;

        private Entity target;
        
        public override string Description => 
            "5초동안 스킬 피해를 (power+1)*15%만큼 상승시킨다. \n" +
            this.statBonusDescription;
        
        public override void trigger(Entity player, float power)
        {
            if (player is Player p)
            {
                lifeTime = p.statCache.additionalDuration + 5;
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
                entityStat.dmgUp[(int)AtkTags.skillDamage] += (power+1)*15;
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