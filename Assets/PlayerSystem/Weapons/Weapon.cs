using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public abstract class Weapon:MonoBehaviour, IEntityEventListener
    {
        protected Player player;
        
        public abstract void fire(AimSupport aimSupport);
        public abstract void skill(AimSupport aimSupport);
        public abstract void ultimate(AimSupport aimSupport);
        public abstract void eventActive(EventArgs eventArgs);

        public void registerTarget(Entity target, object args = null)
        {
            if (target is Player p)
            {
                player = p;
                player.registerListener(this);
            }
            else Debug.LogWarning(target + " is not a player");
        }

        public void removeSelf()
        {
            this.player.removeListener(this);
        }
        public abstract void update(float deltaTime, Entity target);
    }
}