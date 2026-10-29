using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public class GrenadeLauncher : ChargedWeapon
    {
        [Header("Grenade launcher")]
        [SerializeField] private float directCoefficient = 25f;
        [SerializeField] private float burnCoefficient = 8f;
        [SerializeField] private float groundRadius = 2f;
        [SerializeField] private float groundSeconds = 4f;
        [SerializeField, Min(0)] private float groundDamageCoefficient = 4f;
        [SerializeField, Min(0.1f)] private float groundDamageInterval = 0.5f;
        [SerializeField, Min(0.1f)] private float groundBurnSeconds = 2f;
        [SerializeField] private float skillRadius = 6f;
        [SerializeField] private float skillBurnCoefficient = 25f;
        [SerializeField] private float skillBurnSeconds = 6f;
        [SerializeField] private int bombardmentCount = 8;
        [SerializeField] private float bombardmentRadius = 5f;
        [SerializeField] private float bombRadius = 2f;
        [SerializeField] private float bombInterval = 0.25f;
        [SerializeField] private float bombCoefficient = 60f;
        [SerializeField] private float bombDotCoefficient = 20f;
        [Header("Grenade visuals")]
        [SerializeField] private Sprite grenadeSprite;
        [SerializeField] private Sprite bombSprite;
        [SerializeField] private GameObject burningGroundVisualPrefab;
        [SerializeField] private GameObject explosionVisualPrefab;
        [SerializeField] private GameObject bombardmentSmallVisualPrefab;
        [SerializeField] private GameObject bombardmentHeavyVisualPrefab;
        [SerializeField, Min(.05f)] private float bombardmentWarningSeconds = .55f;
        [SerializeField, Min(.1f)] private float bombardmentFallSeconds = .65f;
        [SerializeField, Min(.05f)] private float bombardmentHeavyHoldSeconds = .4f;
        [SerializeField] private AtkTags[] bombardmentElements =
            { AtkTags.heatDamage, AtkTags.electricalDamage, AtkTags.physicalDamage };

        protected override string HudTitle => "유탄 발사기";
        protected override string HudRole => "화상 장판 · 3중첩 화상 · 무작위 지속 피해";
        protected override string SkillTitle => "화염 확산";
        protected override string UltimateTitle => "지원 폭격";

        private void Reset()
        {
            displayName = "Grenade launcher";
            fireInterval = 0.8f;
            maxSkillCharges = 2;
        }

        public override void fire()
        {
            if (!BeginFire()) return;
            var grenade = WeaponProjectile.Create(player, ProjectileOrigin, Direction, 14f, 1.2f, 0,
                new AtkTagSet().Add(AtkTags.heatDamage, AtkTags.normalAttackDamage), 0,
                null, explosionVisualPrefab, 0.25f, 0.7f, grenadeSprite,
                weaponSprite ? weaponSprite.sharedMaterial : null, -90f);
            // The callback belongs to the projectile, not this weapon's update lifetime.
            grenade.Impact = (enemy, point) =>
            {
                if (!player) return;
                DamageArea(point, groundRadius, directCoefficient, AtkTags.normalAttackDamage);
                BurningGround.Create(player, point, groundRadius, groundSeconds, burnCoefficient,
                    burningGroundVisualPrefab, groundDamageCoefficient, groundDamageInterval, groundBurnSeconds,
                    weaponSprite ? weaponSprite.sharedMaterial : null);
            };
        }

        public override void skill()
        {
            if (!BeginSkill()) return;
            WeaponVisualEffects.Play(explosionVisualPrefab, Origin, Quaternion.identity, skillRadius * 0.3f);
            foreach (var enemy in Enemies(Origin, skillRadius))
                WeaponDamageOverTime.Apply(player, enemy, AtkTags.heatDamage,
                    skillBurnCoefficient, skillBurnSeconds, 3);
        }

        public override void ultimate()
        {
            if (!BeginUltimate()) return;
            BombardmentSequence.Launch(new BombardmentSequence.Config
            {
                Owner = player, Center = AimPoint, Count = bombardmentCount,
                ScatterRadius = bombardmentRadius, BlastRadius = bombRadius, Interval = bombInterval,
                Coefficient = bombCoefficient, DotCoefficient = bombDotCoefficient,
                WarningSeconds = bombardmentWarningSeconds, FallSeconds = bombardmentFallSeconds,
                HeavyHoldSeconds = bombardmentHeavyHoldSeconds,
                Elements = bombardmentElements == null ? null : (AtkTags[])bombardmentElements.Clone(),
                BombSprite = bombSprite, Material = weaponSprite ? weaponSprite.sharedMaterial : null,
                SmallVisual = bombardmentSmallVisualPrefab, HeavyVisual = bombardmentHeavyVisualPrefab,
                ExplosionVisual = explosionVisualPrefab, EmbersVisual = burningGroundVisualPrefab
            });
        }

        private void DamageArea(Vector2 point, float radius, float coefficient, AtkTags category)
        {
            var snapshot = player.stat.calculate();
            foreach (var enemy in Enemies(point, radius))
            {
                var tags = new AtkTagSet().Add(AtkTags.heatDamage, category);
                new DamageGiveEvent(snapshot.calculateTrueDamage(tags, coefficient), Vector3.zero,
                    player, enemy, tags, 2f).trigger();
            }
        }
    }
}
