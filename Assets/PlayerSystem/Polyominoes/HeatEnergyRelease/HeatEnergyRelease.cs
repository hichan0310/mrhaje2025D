using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Effects.EnergyGun;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Polyominoes.HeatEnergyRelease
{
    public class HeatEnergyRelease:Polyomino
    {
        public override string Name => "Heat Energy Release";
        public override string Description => "공격력 (10+power*10)%의 열 피해를 주는 유도탄을 6개 발사한다. ";

        public EnergyBullet energyBullet;
        private AtkTagSet atkTagSet = new AtkTagSet().Add(AtkTags.triggerEffectDamage, AtkTags.heatDamage);
        
        public override void trigger(Entity entity, float power)
        {
            var stat = entity.stat.calculate();
            for (int i = 0; i < 6; i++)
            {
                float angle = 60*i+Random.Range(0,60);
                var e=Instantiate(energyBullet, entity.transform.position, Quaternion.AngleAxis(angle, Vector3.forward));
                var tagSet = new AtkTagSet(atkTagSet);
                var dmg = stat.calculateTrueDamage(tagSet, 10f + power * 10f);
                var giveEvent = new DamageGiveEvent(dmg, Vector3.zero, entity, null, tagSet, 1);
                e.damageGiveEvent = giveEvent;
            }
        }
    }
}