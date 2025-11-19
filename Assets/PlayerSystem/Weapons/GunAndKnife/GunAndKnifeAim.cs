using DefaultNamespace;
using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class GunAndKnifeAim:AimSupport
    {
        public float aimDuration { get; set; } = 0;
        [SerializeField] private float slowTime = 0.3f;
        private bool slow = false;
        
        protected override void HandleAimInput()
        {
            // 마우스 왼쪽 버튼을 누르고 있을 때
            if (Input.GetMouseButtonDown(0))
            {
                isAiming = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                this.weapon.fire();
                isAiming = false;
                aimRange = MAX_AIM_RANGE; // 리셋
                aimDuration = 0f;
                if (this.slow)
                {
                    this.slow = false;
                }
            }

            // 조준 중일 때 aimRange 지수적 감소
            if (isAiming)
            {
                if (!this.slow && aimDuration >= slowTime)
                {
                    this.slow = true;
                }
                aimDuration += Time.deltaTime;
                // 지수 감소: aimRange = MIN + (MAX - MIN) * e^(-k*t)
                aimRange = 5 + (35 - MIN_AIM_RANGE) * Mathf.Exp(-AIM_DECAY_RATE * aimDuration);
            }
        }
    }
}