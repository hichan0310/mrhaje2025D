using System.Collections.Generic;
using EntitySystem;

namespace PlayerSystem.Effects
{
    public class EffectPowerExample:ITriggerEffect
    {
        public List<ITriggerEffect> effects = new List<ITriggerEffect>();
        
        public void trigger(Entity entity, float power)
        {
            foreach (ITriggerEffect effect in effects)
                effect.trigger(entity, power*0.7f);
        }
    }
}