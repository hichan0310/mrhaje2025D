using System;
using EntitySystem;
using EntitySystem.Events;
using GameBackend;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.RecoveryManager
{
    public class RecoveryManager : Polyomino
    {
        public override string Name => "RecoveryManager";

        public override string Description =>
            "에너지를 자연적으로 축적하고 내부 에너지는 0부터 2까지 상승한다.\n" +
            "power*1000에 내부 에너지의 제곱을 곱한 값만큼 회복한다. \n" +
            "내부 에너지는 트리거가 발동된 후 1초 이후에 쌓이기 시작한다. \n" +
            this.statBonusDescription;

        private float energy;
        private float rechargeDelay;

        public override void trigger(Entity player, float power)
        {
            if (energy > 0)
            {
                var healAmount = energy * energy * power * 1000;
                new HealEvent(player, (int)healAmount).trigger();
            }
            energy = 0;
            rechargeDelay = 1f;
        }

        private void Update()
        {
            float dt = TimeManager.deltaTime;
            if (rechargeDelay > 0)
            {
                float waiting = System.Math.Min(rechargeDelay, dt);
                rechargeDelay -= waiting;
                dt -= waiting;
            }
            this.energy += dt;
            if (this.energy >= 2) this.energy = 2;
        }
    }
}
