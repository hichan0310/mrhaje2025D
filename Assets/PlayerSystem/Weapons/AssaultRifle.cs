using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public class AssaultRifle : ChargedWeapon
    {
        [Header("Assault rifle")]
        [SerializeField, Min(1)] private int magazineSize = 30;
        [SerializeField, Min(0.1f)] private float reloadSeconds = 1.8f;
        [SerializeField, Min(0)] private float damageCoefficient = 30f;
        [SerializeField, Min(0.1f)] private float ultimateSeconds = 10f;
        [SerializeField, Min(1)] private float ultimateMaxRate = 3f;
        private float reloadRemaining;
        private float ultimateRemaining;
        public int Ammo { get; private set; }
        public bool IsReloading => reloadRemaining > 0;
        protected override float RateMultiplier => ultimateRemaining > 0 ?
            Mathf.Lerp(1f, ultimateMaxRate, 1f - ultimateRemaining / ultimateSeconds) : 1f;
        protected override string HudTitle => "돌격소총";
        protected override string HudRole => "지속 사격 · E 즉시 재장전 · R 재장전";
        protected override string SkillTitle => "즉시 재장전";
        protected override string UltimateTitle => "가속 연사";

        public override void WriteHud(WeaponHudState state)
        {
            base.WriteHud(state);
            if (ultimateRemaining > 0)
                state.Entries[1] = WeaponHudEntry.Ultimate(UltimateTitle, Energy, MaxEnergy,
                    ultimateCost, $"가속 중 · {ultimateRemaining:0.0}초", true);
            state.Entries.Add(new WeaponHudEntry("R", "탄창", $"{Ammo} / {magazineSize}",
                IsReloading ? $"재장전 중 · {reloadRemaining:0.0}초" : "E로 즉시 재장전 가능",
                IsReloading ? 1 - reloadRemaining / Mathf.Max(0.1f, reloadSeconds) : (float)Ammo / Mathf.Max(1, magazineSize),
                WeaponHudAccent.Ammo, active: IsReloading, compact: true));
            if (ultimateRemaining > 0)
                state.Entries.Add(new WeaponHudEntry("", "연사 속도", $"×{RateMultiplier:0.00}",
                    $"가속 종료까지 {ultimateRemaining:0.0}초", 1 - ultimateRemaining / ultimateSeconds,
                    WeaponHudAccent.Effect, active: true, compact: true));
        }

        private void Reset()
        {
            displayName = "Assault rifle";
            automaticFire = true;
            fireInterval = 0.12f;
            maxSkillCharges = 4;
            skillRechargeSeconds = 8;
        }

        public override void registerTarget(Entity target, object args = null)
        {
            maxSkillCharges = 4;
            automaticFire = true;
            base.registerTarget(target, args);
            Ammo = magazineSize;
        }

        protected override void Update()
        {
            base.Update();
            if (CanAct && Input.GetKeyDown(KeyCode.R)) Reload();
        }

        public void Reload()
        {
            if (!CanAct || IsReloading || Ammo >= magazineSize) return;
            reloadRemaining = reloadSeconds;
        }

        public override void fire()
        {
            if (!CanAct || IsReloading) return;
            if (Ammo <= 0) { Reload(); return; }
            if (!BeginFire()) return;
            Ammo--;
            Shoot(damageCoefficient, 1.5f, 45f, Random.Range(-2f, 2f));
            if (Ammo == 0) Reload();
        }

        public override void skill()
        {
            if (!BeginSkill()) return;
            reloadRemaining = 0;
            Ammo = magazineSize;
        }

        public override void ultimate()
        {
            if (ultimateRemaining > 0 || !BeginUltimate()) return;
            ultimateRemaining = ultimateSeconds;
        }

        protected override void TickWeapon(float deltaTime)
        {
            if (reloadRemaining > 0)
            {
                reloadRemaining -= deltaTime;
                if (reloadRemaining <= 0) Ammo = magazineSize;
            }
            ultimateRemaining = Mathf.Max(0, ultimateRemaining - deltaTime);
        }
    }
}
