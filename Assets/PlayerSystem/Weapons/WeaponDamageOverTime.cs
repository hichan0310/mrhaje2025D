using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Each application is an independent stack and remains compatible with FeatureExtractor.</summary>
    public class WeaponDamageOverTime : DamageOverTime
    {
        private Entity owner;
        private float remaining;
        private float interval;
        protected override float cooldown => interval;

        public static void Apply(Entity owner, EnemyBase enemy, AtkTags element, float coefficient,
            float seconds, int stacks = 1, float tickSeconds = 1f)
        {
            for (int i = 0; i < stacks; i++)
                CreateStack(owner, enemy, element, coefficient, seconds, tickSeconds);
        }

        /// <summary>Refresh one field's burn without adding stacks or restarting the tick clock.</summary>
        public static WeaponDamageOverTime RefreshOrApply(WeaponDamageOverTime existing, Entity owner,
            EnemyBase enemy, AtkTags element, float coefficient, float seconds, float tickSeconds = 1f)
        {
            if (!owner || owner.stat == null || !enemy || enemy.stat == null || enemy.stat.nowHp <= 0) return null;
            if (!existing || existing.owner != owner || existing.target != enemy || existing.remaining <= 0)
                return CreateStack(owner, enemy, element, coefficient, seconds, tickSeconds);
            existing.remaining = Mathf.Max(existing.remaining, seconds);
            existing.damageGiveEvent = MakeDamage(owner, enemy, element, coefficient);
            return existing;
        }

        private static WeaponDamageOverTime CreateStack(Entity owner, EnemyBase enemy, AtkTags element,
            float coefficient, float seconds, float tickSeconds)
        {
            if (!owner || owner.stat == null || !enemy || enemy.stat == null || enemy.stat.nowHp <= 0) return null;
            var go = new GameObject(element + " DoT");
            go.transform.SetParent(enemy.transform, false);
            var dot = go.AddComponent<WeaponDamageOverTime>();
            dot.owner = owner;
            dot.remaining = Mathf.Max(0, seconds);
            dot.interval = Mathf.Max(0.1f, tickSeconds);
            dot.damageGiveEvent = MakeDamage(owner, enemy, element, coefficient);
            dot.registerTarget(enemy);
            return dot;
        }

        private static DamageGiveEvent MakeDamage(Entity owner, EnemyBase enemy, AtkTags element, float coefficient)
        {
            var tags = new AtkTagSet().Add(element, AtkTags.damageOverTime, AtkTags.notcriticalHit);
            int damage = owner.stat.calculate().calculateTrueDamage(tags, coefficient);
            return new DamageGiveEvent(damage, Vector3.zero, owner, enemy, tags, 0);
        }

        public override void update(float deltaTime, Entity entity)
        {
            if (entity != target) return;
            if (!owner || !target || target.stat.nowHp <= 0) { Destroy(gameObject); return; }
            float activeTime = Mathf.Min(deltaTime, remaining);
            timer += activeTime;
            remaining -= deltaTime;
            while (timer >= interval && target && target.stat.nowHp > 0)
            {
                timer -= interval;
                giveDamage();
            }
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
