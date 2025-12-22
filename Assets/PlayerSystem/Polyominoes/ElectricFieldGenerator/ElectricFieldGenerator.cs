using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Polyominoes.ElectricFieldGenerator
{
    public class ElectricFieldGenerator: Polyomino
    {
        public override string Name=> "ElectricFieldGenerator";
        [SerializeField] private ElectricField electricField;

        public override string Description =>
            "순간적으로 전류를 증폭시켜 주변 넓은 범위에 자기, 전기장 영역을 전개하고 전기 속성 피해와 함께 마비 효과를 부여한다. \n" +
            "파워가 2보다 커야 마비 효과를 부여할 수 있다. 마비 효과 수치는 power*30-50%로 10초동안 적용된다. \n" +
            "마비 효과: 마비 효과가 부여된 적은 지속 피해, 전투 모듈 피해가 증가한다. 지속 피해는 1.5초에 1번 들어간다. \n" +
            "마비 효과 자체는 중첩될 수 있지만 마비 효과의 피해 증가 효과는 중첩될 수 없다. \n" +
            this.statBonusDescription;
        
        public override void trigger(Entity player, float power)
        {
            var e=Instantiate(electricField, player.transform.position, Quaternion.identity);

            var stat = player.stat.calculate();
            e.entityStat=stat;
            e.power=power;
        }
    }
}