using System;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using GameBackend;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Swept projectile with a visual-only child asset; damage and movement stay here.</summary>
    public class WeaponProjectile : MonoBehaviour
    {
        private Entity owner;
        private EntityStat snapshot;
        private Vector2 velocity;
        private float lifetime;
        private float coefficient;
        private float energy;
        private AtkTagSet tags;
        private bool finished;
        private GameObject impactVisualPrefab;
        private float impactVisualScale;
        public Action<EnemyBase, Vector2> Impact;

        public static WeaponProjectile Create(Entity owner, Vector2 origin, Vector2 direction, float speed,
            float lifetime, float coefficient, AtkTagSet tags, float energy,
            GameObject visualPrefab = null, GameObject impactPrefab = null,
            float visualScale = 0.2f, float impactScale = 0.3f, Sprite sprite = null,
            Material spriteMaterial = null, float spriteAngleOffset = 0f)
        {
            var go = new GameObject("Weapon projectile");
            go.transform.position = origin;
            var projectile = go.AddComponent<WeaponProjectile>();
            projectile.owner = owner;
            projectile.snapshot = owner.stat.calculate();
            projectile.velocity = direction.normalized * speed;
            projectile.lifetime = lifetime;
            projectile.coefficient = coefficient;
            projectile.tags = new AtkTagSet(tags);
            projectile.energy = energy;
            projectile.impactVisualPrefab = impactPrefab;
            projectile.impactVisualScale = impactScale;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (visualPrefab)
            {
                var visual = Instantiate(visualPrefab, go.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0, 90, 0);
                visual.transform.localScale = Vector3.one * visualScale;
                foreach (var renderer in visual.GetComponentsInChildren<ParticleSystemRenderer>())
                    renderer.sortingOrder = 15;
            }
            if (sprite)
            {
                var image = new GameObject("Projectile sprite");
                image.transform.SetParent(go.transform, false);
                image.transform.localRotation = Quaternion.Euler(0, 0, spriteAngleOffset);
                var renderer = image.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                if (spriteMaterial) renderer.sharedMaterial = spriteMaterial;
                renderer.sortingOrder = 15;
                image.transform.localScale = Vector3.one * (visualScale / Mathf.Max(0.01f, sprite.bounds.size.x));
            }
            if (!visualPrefab && !sprite)
            {
                var line = go.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = line.endColor = Color.yellow;
                line.startWidth = line.endWidth = 0.08f;
                line.positionCount = 2;
                line.useWorldSpace = false;
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, Vector3.left * 0.3f);
            }
            return projectile;
        }

        private void Update()
        {
            if (finished) return;
            if (!owner) { Destroy(gameObject); return; }
            float dt = TimeManager.deltaTime;
            if (dt <= 0) return;
            var from = (Vector2)transform.position;
            float distance = velocity.magnitude * dt;
            var hits = Physics2D.CircleCastAll(from, 0.08f, velocity.normalized, distance);
            foreach (var hit in hits)
            {
                var entity = hit.collider.GetComponentInParent<Entity>();
                if (entity == owner || entity is Player || (entity && !(entity is EnemyBase))) continue;
                var enemy = entity as EnemyBase;
                if (enemy && (!enemy.isActiveAndEnabled || enemy.stat == null || enemy.stat.nowHp <= 0)) continue;
                if (!enemy && hit.collider.isTrigger) continue;
                Finish(enemy, hit.centroid);
                return;
            }
            transform.position = from + velocity * dt;
            lifetime -= dt;
            if (lifetime <= 0) Finish(null, transform.position);
        }

        private void Finish(EnemyBase target, Vector2 position)
        {
            finished = true;
            transform.position = position;
            WeaponVisualEffects.Play(impactVisualPrefab, position, Quaternion.identity, impactVisualScale);
            if (target && coefficient > 0)
            {
                var hitTags = new AtkTagSet(tags);
                int damage = snapshot.calculateTrueDamage(hitTags, coefficient);
                new DamageGiveEvent(damage, Vector3.zero, owner, target, hitTags, energy).trigger();
            }
            if (owner) Impact?.Invoke(target, position);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            var line = GetComponent<LineRenderer>();
            if (line && line.sharedMaterial) Destroy(line.sharedMaterial);
        }
    }
}
