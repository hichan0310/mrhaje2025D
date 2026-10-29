using System.Collections.Generic;
using EntitySystem;
using PlayerSystem.Tiling;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace PlayerSystem.Polyominoes.ExecutionFollowupShot
{
    public class ExecutionFollowupShot : Polyomino
    {
        public override string Name => "Execution Followup Shot";

        public override string Description =>
            "발동 시 주변에 전기 구체를 소환하여 전기 구체는 플레이어 주위를 회전한다. \n" +
            "다음 일반 공격이 명중한 적에게 번개를 날려서 고정 피해 500을 가한다. \n" +
            "최대 20번 중첩되고 중첩한 횟수만큼 번개가 한번에 날아간다. \n" +
            this.statBonusDescription;

        private List<Shot> shots = new List<Shot>();
        [SerializeField] private Shot shotObject;

        public override void trigger(Entity entity, float power)
        {
            if (shots.Count >= 20) return;
            var shot = Instantiate(shotObject);
            shots.Add(shot);
            shot.registerTarget(entity);
            shot.shots = this.shots;
        }
    }
}
