using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;

namespace PlayerSystem
{
    public abstract class Trigger:IEntityEventListener
    {
        protected Entity entity { get; set; }
        protected List<ITriggerEffect> effects { get; set; } = new List<ITriggerEffect>();
        public abstract void eventActive(EventArgs eventArgs);
        public abstract void registerTarget(Entity target, object args = null);
        public abstract void removeSelf();
        public abstract void update(float deltaTime, Entity target);
    }
}