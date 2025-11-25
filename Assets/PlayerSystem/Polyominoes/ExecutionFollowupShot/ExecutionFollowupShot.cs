using System.Collections.Generic;
using EntitySystem;
using PlayerSystem.Tiling;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace PlayerSystem.Polyominoes.ExecutionFollowupShot
{
    public class ExecutionFollowupShot:Polyomino
    {
        public override string Name => "Execution Followup Shot";
        public override string Description => "다음 일반 공격이 명중한 적에게 고정 피해 500 추가 사격\n" +
                                              "최대 20번 중첩 가능하다. ";
        private List<Shot> shots = new List<Shot>();
        [SerializeField] private Shot shotObject;
        public override void trigger(Entity entity, float power)
        {
            if(shots.Count>=20) return;
            var shot = Instantiate(shotObject);
            shot.registerTarget(entity);
            shot.shots = this.shots;
        }
    }
}