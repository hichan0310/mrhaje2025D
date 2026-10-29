using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class Mark : MonoBehaviour, IEntityEventListener
    {
        [SerializeField] private float duration = 20f;
        [SerializeField] private float explosionCoefficient = 600f;
        private class Marking
        {
            public float time;
            public int requireHit;
            public GameObject effect;
        }
        public Player player { get; set; }
        public GameObject markingEffect;
        private readonly Dictionary<Entity, Marking> targets = new();

        public void MakeOneHitInArea(Vector2 center, float radius)
        {
            foreach (var entry in targets)
                if (entry.Key && ((Vector2)entry.Key.transform.position - center).sqrMagnitude <= radius * radius)
                    entry.Value.requireHit = 1;
        }

        public void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is EntityDieEvent death) { Remove(death.entity); return; }
            if (eventArgs is not DamageTakeEvent hit || !player || hit.attacker != player ||
                !hit.atkTags.Contains(AtkTags.normalAttackDamage) || !targets.TryGetValue(hit.target, out var mark)) return;
            if (--mark.requireHit > 0) return;
            Remove(hit.target);
            if (!hit.target || hit.target.stat.nowHp <= 0) return;
            var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.skillDamage);
            new DamageGiveEvent(player.stat.calculate().calculateTrueDamage(tags, explosionCoefficient),
                Vector3.zero, player, hit.target, tags, 15).trigger();
        }

        public void registerTarget(Entity target, object args = null)
        {
            if (!player || target is not EnemyBase || target.stat == null || target.stat.nowHp <= 0) return;
            if (targets.TryGetValue(target, out var existing)) { existing.time += duration; return; }
            var stat = player.stat.calculate();
            var mark = new Marking
            {
                time = duration,
                requireHit = Mathf.Max(1, (int)((stat.bulletRate * stat.fireSpeed + 2) * 3))
            };
            if (markingEffect)
                mark.effect = Instantiate(markingEffect, target.transform.position + Vector3.back,
                    Quaternion.identity, target.transform);
            targets.Add(target, mark);
            target.registerListener(this);
        }

        private void Remove(Entity target)
        {
            if (!targets.TryGetValue(target, out var mark)) return;
            if (mark.effect) Destroy(mark.effect);
            targets.Remove(target);
            if (target) target.removeListener(this);
        }

        public void removeSelf()
        {
            foreach (var target in new List<Entity>(targets.Keys)) Remove(target);
        }

        public void update(float deltaTime, Entity target)
        {
            if (!targets.TryGetValue(target, out var mark)) return;
            mark.time -= deltaTime;
            if (!player || mark.time <= 0) Remove(target);
        }

        private void OnDestroy() => removeSelf();
    }
}
