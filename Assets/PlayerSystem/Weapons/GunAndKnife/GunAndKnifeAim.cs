using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    /// <summary>Direct click fire. Multi-shot spread belongs to the weapon, not a charge timer.</summary>
    public class GunAndKnifeAim : AimSupport
    {
        protected override void HandleAimInput()
        {
            isAiming = false;
            if (Input.GetMouseButtonDown(0))
            {
                // Read the current mouse position before firing rather than the previous frame's target.
                UpdateTargeting();
                if (weapon) weapon.fire();
            }
        }
    }
}
