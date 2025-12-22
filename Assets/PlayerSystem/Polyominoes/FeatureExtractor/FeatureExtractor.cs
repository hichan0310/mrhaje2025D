using System;
using EntitySystem;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Polyominoes.FeatureExtractor
{
    public class FeatureExtractor:Polyomino
    {
        public override string Name => "FeatureExtractor";
        public override string Description => 
            "1 이상의 파워가 들어올 경우 플레이어 주변의 적 상태를 분석하여 부여된 지속 피해를 1회 추가로 가한다. \n" +
            this.statBonusDescription;
        
        [SerializeField] private FeatureExtractionExecutor featureExtractionExecutor;
        
        public override void trigger(Entity player, float power)
        {
            if(power<1) return;
            var f = Instantiate(featureExtractionExecutor);
            f.init(player, power);
        }
    }
}