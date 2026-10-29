using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Particle lifetime measured in real seconds while the world is stopped.</summary>
    public sealed class WeaponVfxLifetime : MonoBehaviour
    {
        public float Remaining;
        private void Update()
        {
            Remaining -= Time.unscaledDeltaTime;
            if (Remaining <= 0) Destroy(gameObject);
        }
    }
}
