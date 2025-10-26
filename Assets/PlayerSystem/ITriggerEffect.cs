using EntitySystem;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem
{
    public interface ITriggerEffect
    {
        public void trigger(Entity entity, float power);
    }
}