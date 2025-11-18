using EntitySystem;
using EntitySystem.Events;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class GunAndKnife:Weapon
    {
        public float fireCooldownNormal;
        public float fireCooldownUltimate;
        public float skillCooldown;
        public float maxSkillStack;
        public float ultimateDuration;
        public int requireEnergy;
        public int maxEnergy;
        
        private float fireTimer;
        private float skillTimer=0;
        private int skillStack;
        private bool isUltimate;
        private float ultimateTimer=0;
        private float energy=0;
        
        
        
        
        public override void fire()
        {
            if (fireTimer <= 0)
            {
                var stat = player.stat.calculate();
                
            }
            
        }

        public override void skill()
        {
            if (skillStack > 0)
            {
                var stat = player.stat.calculate();
            }
        }

        public override void ultimate()
        {
            if (energy >= requireEnergy)
            {
                
            }
        }

        public override void eventActive(EventArgs eventArgs)
        {
            switch (eventArgs)
            {
                case DamageGiveEvent damageGiveEvent:
                {
                    this.energy += player.statCache.energyRecharge * damageGiveEvent.energeRecharge;
                    break;
                }
                case JustDodgeEvent justDodgeEvent:
                {
                    this.energy += player.statCache.energyRecharge * 5;
                    break;
                }
                case DamageTakeEvent damageTakeEvent:
                {
                    this.energy += player.statCache.energyRecharge * 1;
                    break;
                }
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            fireTimer -= deltaTime;
            if (fireTimer < 0) fireTimer = 0;

            if (skillStack < maxSkillStack)
            {
                skillTimer -= deltaTime;
                if (skillTimer < 0)
                {
                    skillTimer += skillCooldown;
                    skillStack += 1;
                }
            }

            ultimateTimer -= deltaTime;
            if (ultimateTimer < 0) ultimateTimer = 0;
        }
    }
}