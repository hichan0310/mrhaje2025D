using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class Mark:MonoBehaviour, IEntityEventListener
    {
        private Entity entity;
        public int requireHit;
        public DamageGiveEvent damageGiveEvent { get; set; }


        public void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageTakeEvent d)
            {
                if (d.atkTags.Contains(AtkTags.normalAttackDamage))
                {
                    requireHit -= 1;
                }
            }
        }

        public void registerTarget(Entity target, object args = null)
        {
            this.entity = target;
            entity.registerListener(this);
        }

        public void removeSelf()
        {
            entity.removeListener(this);
        }

        public void update(float deltaTime, Entity target)
        {
            
        }
    }
}