using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class ChainLoadingProtocol:Board
    {
        private int charge = 0;
        private float timer = 0;
        
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is BasicAttackExecuteEvent)
            {
                charge += 1;
            }
            else if (eventArgs is DamageGiveEvent damageGiveEvent)
            {
                if (damageGiveEvent.atkTags.Contains(AtkTags.skillDamage))
                {
                    this.trigger(0.2f*this.charge+0.5f);
                    timer = 1;
                    if (this.entity is Player player)
                    {
                        timer = 2f / (player.statCache.fireSpeed + 1);
                    }
                }
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            if (target == this.entity)
            {
                timer -= deltaTime;
                if (timer <= 0)
                {
                    timer = 0;
                }
            }
        }
    }
}