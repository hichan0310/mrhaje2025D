using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using GameBackend;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Periodic damage on contact, plus one refreshed burn per field and enemy.</summary>
    public class BurningGround : MonoBehaviour
    {
        private Entity owner;
        private float radius, remaining, elapsed, duration;
        private float burnCoefficient, groundCoefficient, damageInterval, burnSeconds;
        private readonly Dictionary<EnemyBase, Exposure> exposures = new();
        private readonly HashSet<EnemyBase> inside = new();
        private readonly List<Collider2D> overlaps = new();
        private ContactFilter2D filter;

        private sealed class Exposure
        {
            public float NextDamage;
            public bool WasInside;
            public WeaponDamageOverTime Burn;
        }

        public static BurningGround Create(Entity owner, Vector2 point, float radius, float seconds, float coefficient,
            GameObject visualPrefab = null, float groundCoefficient = 4f, float damageInterval = 0.5f,
            float burnSeconds = 2f, Material material = null)
        {
            var go = new GameObject("Burning ground");
            go.transform.position = point;
            var field = go.AddComponent<BurningGround>();
            field.owner = owner;
            field.radius = Mathf.Max(0.1f, radius);
            field.remaining = field.duration = Mathf.Max(0.1f, seconds);
            field.burnCoefficient = Mathf.Max(0, coefficient);
            field.groundCoefficient = Mathf.Max(0, groundCoefficient);
            field.damageInterval = Mathf.Max(0.1f, damageInterval);
            field.burnSeconds = Mathf.Max(0.1f, burnSeconds);
            field.filter = new ContactFilter2D().NoFilter();
            field.filter.useTriggers = true;
            if (visualPrefab)
            {
                var visual = Instantiate(visualPrefab, go.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * field.radius;
                foreach (var renderer in visual.GetComponentsInChildren<ParticleSystemRenderer>())
                    renderer.sortingOrder = 4;
            }
            if (material)
                WeaponVfxStroke.Ring("Burning ground edge", point, field.radius, material,
                    new Color(1f, 0.3f, 0.05f, 0.7f), 0.04f, go.transform);
            return field;
        }

        private void Update() => Advance(TimeManager.deltaTime);

        private void Advance(float deltaTime)
        {
            if (!owner || owner.stat == null || remaining <= 0) { Destroy(gameObject); return; }
            if (deltaTime <= 0) return;
            elapsed += Mathf.Min(deltaTime, remaining);
            remaining -= deltaTime;
            inside.Clear();
            overlaps.Clear();
            Physics2D.OverlapCircle(transform.position, radius, filter, overlaps);
            foreach (var collider in overlaps)
            {
                var enemy = collider.GetComponentInParent<EnemyBase>();
                if (enemy && enemy.isActiveAndEnabled && enemy.stat != null && enemy.stat.nowHp > 0)
                    inside.Add(enemy);
            }
            foreach (var enemy in inside)
            {
                if (!exposures.TryGetValue(enemy, out var exposure))
                {
                    exposure = new Exposure();
                    exposures.Add(enemy, exposure);
                }
                // Re-entry gives a fresh contact pulse, never catch-up damage for time spent outside.
                if (!exposure.WasInside) exposure.NextDamage = elapsed;
                while (exposure.NextDamage <= elapsed && exposure.NextDamage < duration && enemy && enemy.stat.nowHp > 0)
                {
                    exposure.NextDamage += damageInterval;
                    var tags = new AtkTagSet().Add(AtkTags.heatDamage, AtkTags.damageOverTime, AtkTags.notcriticalHit);
                    int damage = owner.stat.calculate().calculateTrueDamage(tags, groundCoefficient);
                    new DamageGiveEvent(damage, Vector3.zero, owner, enemy, tags, 0).trigger();
                    if (enemy && enemy.stat.nowHp > 0)
                        exposure.Burn = WeaponDamageOverTime.RefreshOrApply(exposure.Burn, owner, enemy,
                            AtkTags.heatDamage, burnCoefficient, burnSeconds);
                }
            }
            foreach (var entry in exposures) entry.Value.WasInside = inside.Contains(entry.Key);
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
