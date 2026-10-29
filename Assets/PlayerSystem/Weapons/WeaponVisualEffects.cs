using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Plays visual-only prefab assets without introducing another damage or movement driver.</summary>
    public static class WeaponVisualEffects
    {
        public static GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation,
            float scale = 1f, float lifetime = 2f, bool unscaled = false)
        {
            if (!prefab) return null;
            var effect = Object.Instantiate(prefab, position, rotation);
            effect.transform.localScale *= scale;
            foreach (var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>())
                renderer.sortingOrder = 20;
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>())
            {
                if (unscaled) { var main = particles.main; main.useUnscaledTime = true; }
                particles.Play();
            }
            if (unscaled) effect.AddComponent<WeaponVfxLifetime>().Remaining = lifetime;
            else Object.Destroy(effect, lifetime);
            return effect;
        }
    }
}
