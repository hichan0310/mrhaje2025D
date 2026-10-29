using System;

namespace PlayerSystem.Weapons
{
    /// <summary>Rules that do not depend on scene objects.</summary>
    public static class WeaponRules
    {
        public static int[] AllocateShotguns(int targetCount, int gunCount = 5)
        {
            int count = Math.Max(0, Math.Min(Math.Max(0, gunCount), targetCount));
            var result = new int[count];
            for (int i = 0; count > 0 && i < gunCount; i++) result[i % count]++;
            return result;
        }

        public static int ProjectileCount(float bulletRate) => Math.Max(1, (int)Math.Floor(bulletRate));

        public static int ShotgunCount(float bulletRate) => 5 + ProjectileCount(bulletRate) - 1;

        // The whole volley receives one small base energy budget, independent of pellet count.
        public static float ShotgunPelletEnergy(bool charged, int pellets, float normalVolleyEnergy, float chargedPelletEnergy)
            => charged ? Math.Max(0, chargedPelletEnergy) : Math.Max(0, normalVolleyEnergy) / Math.Max(1, pellets);

        public static float FireInterval(float baseInterval, float fireSpeed, float rampMultiplier = 1f)
        {
            return Math.Max(0.02f, baseInterval / Math.Max(0.1f, fireSpeed) / Math.Max(1f, rampMultiplier));
        }
    }
}
