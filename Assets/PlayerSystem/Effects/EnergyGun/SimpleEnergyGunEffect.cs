using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Effects.EnergyGun
{
    public class SimpleEnergyGunEffect:MonoBehaviour, ITriggerEffect
    {
        public EnergyBullet energyBullet;
        
        public void trigger(Entity entity, float power)
        {
            var stat = entity.stat.calculate();
            for (int i = 0; i < 6; i++)
            {
                float angle = 60*i;
                var e=Instantiate(energyBullet, entity.transform.position, Quaternion.AngleAxis(angle, Vector3.forward));
                e.stat = stat;
                e.power = power;
            }
        }
    }
}