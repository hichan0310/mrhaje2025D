using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Shared input, cooldown, skill charges and energy for the new weapon prototypes.</summary>
    public abstract class ChargedWeapon : Weapon
    {
        [Header("Weapon")]
        [SerializeField] protected string displayName = "Weapon";
        [SerializeField] protected bool automaticFire;
        [SerializeField, Min(0.02f)] protected float fireInterval = 0.5f;
        [SerializeField, Min(1)] protected int maxSkillCharges = 1;
        [SerializeField, Min(0.1f)] protected float skillRechargeSeconds = 6f;
        [SerializeField, Min(1f)] protected float ultimateCost = 60f;
        [SerializeField, Min(1f)] protected float maxEnergy = 100f;

        [Header("Visuals")]
        [SerializeField] protected Transform visualRoot;
        [SerializeField] protected SpriteRenderer weaponSprite;
        [SerializeField] protected Transform firePoint;
        [SerializeField] protected GameObject muzzleVisualPrefab;
        [SerializeField] protected GameObject projectileVisualPrefab;
        [SerializeField] protected GameObject impactVisualPrefab;
        [SerializeField, Min(0.01f)] protected float muzzleVisualScale = 0.35f;
        [SerializeField, Min(0.01f)] protected float projectileVisualScale = 0.2f;
        [SerializeField, Min(0.01f)] protected float impactVisualScale = 0.3f;

        protected float fireRemaining;
        private float chargeRemaining;
        private float chargeDuration;
        protected virtual string HudTitle => displayName;
        protected virtual string HudRole => "일반 공격 · 스킬 · 필살기";
        protected virtual string SkillTitle => "스킬";
        protected virtual string UltimateTitle => "필살기";
        protected virtual string HudBlock => "";
        public int SkillCharges { get; private set; }
        public float Energy { get; private set; }
        public float MaxEnergy => Mathf.Max(ultimateCost, maxEnergy);
        protected virtual bool CanAct => isActiveAndEnabled && player && !player.InputSuppressed && player.stat != null && player.stat.nowHp > 0;
        protected virtual float RateMultiplier => 1f;
        protected Vector2 AimPoint => aimSupport ? (Vector2)aimSupport.target :
            Camera.main ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : Origin + Vector2.right;
        protected Vector2 Origin => (Vector2)player.transform.position;
        protected Vector2 MuzzlePosition => firePoint ? (Vector2)firePoint.position : Origin;
        protected Vector2 ProjectileOrigin
        {
            get
            {
                // A long barrel must not spawn a projectile beyond a wall or a nearby enemy.
                foreach (var hit in Physics2D.LinecastAll(Origin, MuzzlePosition))
                {
                    var entity = hit.collider.GetComponentInParent<Entity>();
                    if (entity == player || entity is Player || (hit.collider.isTrigger && !entity)) continue;
                    return hit.point;
                }
                return MuzzlePosition;
            }
        }
        protected Vector2 Direction => (AimPoint - Origin).sqrMagnitude > 0.001f ?
            (AimPoint - Origin).normalized : Vector2.right;

        public override void registerTarget(Entity target, object args = null)
        {
            base.registerTarget(target, args);
            SkillCharges = Mathf.Max(1, maxSkillCharges);
            chargeRemaining = chargeDuration = RechargeDuration;
        }

        private float RechargeDuration => Mathf.Max(0.1f,
            skillRechargeSeconds * 4f / Mathf.Max(0.1f, player.stat.calculate().skillCooldownDecrease + 4f));

        protected virtual void Update()
        {
            if (!CanAct) return;
            transform.position = player.transform.position;
            Vector2 aim = AimPoint - Origin;
            if (aim.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
                if (visualRoot) visualRoot.localScale = new Vector3(1f, aim.x < 0 ? -1f : 1f, 1f);
            }
            if (Input.GetKeyDown(KeyCode.E)) skill();
            if (Input.GetKeyDown(KeyCode.Q)) ultimate();
            if (automaticFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0)) fire();
        }

        public override void update(float deltaTime, Entity target)
        {
            if (target != player || !CanAct) return;
            fireRemaining = Mathf.Max(0, fireRemaining - deltaTime);
            if (SkillCharges < maxSkillCharges)
            {
                chargeRemaining -= deltaTime;
                while (chargeRemaining <= 0 && SkillCharges < maxSkillCharges)
                {
                    SkillCharges++;
                    chargeDuration = RechargeDuration;
                    chargeRemaining += chargeDuration;
                }
            }
            TickWeapon(deltaTime);
        }

        protected virtual void TickWeapon(float deltaTime) { }

        protected bool BeginFire()
        {
            if (!CanAct || fireRemaining > 0) return false;
            fireRemaining = WeaponRules.FireInterval(fireInterval, player.stat.calculate().fireSpeed, RateMultiplier);
            new BasicAttackExecuteEvent(player, AimPoint).trigger();
            WeaponVisualEffects.Play(muzzleVisualPrefab, MuzzlePosition, transform.rotation,
                muzzleVisualScale, 0.6f);
            return true;
        }

        protected bool BeginSkill()
        {
            if (!CanAct || SkillCharges <= 0) return false;
            if (SkillCharges == maxSkillCharges) chargeRemaining = chargeDuration = RechargeDuration;
            SkillCharges--;
            new SkillExecuteEvent(player, null).trigger();
            return true;
        }

        protected bool BeginUltimate()
        {
            if (!CanAct || Energy < ultimateCost) return false;
            Energy -= ultimateCost;
            new UltimateExecuteEvent(player, Mathf.RoundToInt(ultimateCost)).trigger();
            return true;
        }

        protected void GainEnergy(float amount)
        {
            if (player && player.stat != null)
                Energy = Mathf.Clamp(Energy + Mathf.Max(0, amount) * player.stat.calculate().energyRecharge, 0, MaxEnergy);
        }

        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent hit && hit.attacker == player) GainEnergy(hit.energeRecharge);
        }

        protected List<EnemyBase> Enemies(Vector2 center, float radius)
        {
            var result = new List<EnemyBase>();
            foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                if (enemy.isActiveAndEnabled && enemy.stat != null && enemy.stat.nowHp > 0 &&
                    ((Vector2)enemy.transform.position - center).sqrMagnitude <= radius * radius)
                    result.Add(enemy);
            return result;
        }

        protected WeaponProjectile Shoot(float coefficient, float energy, float speed = 30f,
            float angleOffset = 0, AtkTags category = AtkTags.normalAttackDamage)
        {
            var direction = (Vector2)(Quaternion.Euler(0, 0, angleOffset) * Direction);
            return WeaponProjectile.Create(player, ProjectileOrigin, direction, speed, 3f, coefficient,
                new AtkTagSet().Add(AtkTags.physicalDamage, category), energy,
                projectileVisualPrefab, impactVisualPrefab, projectileVisualScale, impactVisualScale);
        }

        protected GameObject CreateFloatingWeapon(Vector3 position)
        {
            var go = new GameObject("Ultimate shotgun");
            go.transform.position = position;
            if (weaponSprite && weaponSprite.sprite)
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = weaponSprite.sprite;
                renderer.sharedMaterial = weaponSprite.sharedMaterial;
                renderer.sortingLayerID = weaponSprite.sortingLayerID;
                renderer.sortingOrder = weaponSprite.sortingOrder + 2;
                float scale = 1.4f / Mathf.Max(0.01f, renderer.sprite.bounds.size.x);
                go.transform.localScale = Vector3.one * scale;
            }
            return go;
        }

        public override void WriteHud(WeaponHudState state)
        {
            base.WriteHud(state);
            state.Title = HudTitle;
            state.Subtitle = HudRole;
            if (!state.Icon && weaponSprite) state.Icon = weaponSprite.sprite;
            state.Entries.Add(WeaponHudEntry.Skill(SkillTitle, SkillCharges, maxSkillCharges,
                chargeRemaining, chargeDuration, HudBlock));
            state.Entries.Add(WeaponHudEntry.Ultimate(UltimateTitle, Energy, MaxEnergy, ultimateCost, HudBlock));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
    }
}
