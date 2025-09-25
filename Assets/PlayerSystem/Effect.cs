using EntitySystem;
using UnityEngine;

namespace PlayerSystem
{
    public interface ITriggerEffect
    {
        public void trigger(Entity entity, float power);
    }
}