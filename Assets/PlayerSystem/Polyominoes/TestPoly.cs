using EntitySystem;
using PlayerSystem.Effects.EnergyGun;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Polyominoes
{
    public class TestPoly:Polyomino
    {
        public EnergyBullet energyBullet;
        
        public override void trigger(Entity entity, float power)
        {
            Debug.Log("triggered");
            var stat = entity.stat.calculate();
            for (int i = 0; i < 6; i++)
            {
                float angle = 60*i+Random.Range(0,60);
                var e=Instantiate(energyBullet, entity.transform.position, Quaternion.AngleAxis(angle, Vector3.forward));
                e.stat = stat;
                e.power = power;
            }
        }
    }
}