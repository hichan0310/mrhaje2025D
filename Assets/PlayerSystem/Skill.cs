using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem
{
    public interface ISkill : IEntityEventListener
    {
        public string skillName { get; }
        public string description { get; }
        public float timeleft { get; set; }
        public void execute();
    }

    public abstract class Skill : MonoBehaviour, ISkill
    {
        protected Player player;
        public abstract void eventActive(EventArgs eventArgs);

        public void registerTarget(Entity target, object args = null)
        {
            if (target is Player player)
                this.player = player;
            this.player.registerListener(this);
        }

        public void removeSelf()
        {
            player.removeListener(this);
        }

        public abstract void update(float deltaTime, Entity entity);

        public abstract string skillName { get; }
        public abstract string description { get; }
        public abstract float timeleft { get; set; }
        public abstract void execute();
    }

    public abstract class Ultimate : Skill
    {
        public abstract int energyCost { get; }
        public abstract int nowEnergy { get; }
    }
}