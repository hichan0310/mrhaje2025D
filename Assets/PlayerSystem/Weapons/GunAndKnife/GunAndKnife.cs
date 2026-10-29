using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class GunAndKnife : Weapon
    {
        public float fireCooldownNormal = 0.2f;
        public float fireCooldownUltimate;
        public float skillCooldown = 6f;
        public float maxSkillStack = 2;
        public float ultimateDuration;
        public int requireEnergy = 60;
        public int maxEnergy = 100;
        [SerializeField] private Bullet bullet;
        [SerializeField] private KnifeSkill knifeSkill;
        [SerializeField] private GameObject firePoint;
        public GameObject muzzleFlashPrefab;
        public Mark marking;
        [SerializeField, Min(0)] private float spreadDegrees = 20f;
        [SerializeField, Min(0)] private float bulletCoefficient = 50f;
        [SerializeField] private float skillRadius = 10f;
        [SerializeField] private float ultimateRadius = 6f;
        private float fireTimer;
        private float skillTimer;
        private float skillDuration;
        private float RechargeDuration => Mathf.Max(0.1f, skillCooldown * 4f /
            Mathf.Max(0.1f, player.stat.calculate().skillCooldownDecrease + 4f));
        public int SkillStack { get; private set; }
        public float Energy { get; private set; }

        public override void registerTarget(Entity target, object args = null)
        {
            base.registerTarget(target, args);
            if (!player) return;
            if (marking && marking.transform.IsChildOf(transform)) marking.removeSelf();
            else if (marking) marking = Instantiate(marking, transform);
            else marking = new GameObject("Marks").AddComponent<Mark>();
            marking.transform.SetParent(transform, false);
            marking.player = player;
            SkillStack = Mathf.Max(1, (int)maxSkillStack);
            skillTimer = skillDuration = RechargeDuration;
        }

        public override void fire()
        {
            if (!isActiveAndEnabled || !player || player.InputSuppressed || player.stat == null || player.stat.nowHp <= 0 || !bullet || fireTimer > 0) return;
            fireTimer = Mathf.Max(0.02f, fireCooldownNormal / Mathf.Max(0.1f, player.stat.calculate().fireSpeed));
            new BasicAttackExecuteEvent(player, aimSupport.target).trigger();
            var stat = player.stat.calculate();
            Vector2 origin = firePoint ? (Vector2)firePoint.transform.position : (Vector2)player.transform.position;
            Vector2 direction = (Vector2)aimSupport.target - origin;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int count = WeaponRules.ProjectileCount(stat.bulletRate);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0 : Mathf.Lerp(-spreadDegrees / 2,
                    spreadDegrees / 2, (float)i / (count - 1));
                float radians = (angle + offset) * Mathf.Deg2Rad;
                var instance = Instantiate(bullet, origin, Quaternion.Euler(0, 0, angle + offset));
                instance.direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);
                float coefficient = bulletCoefficient;
                instance.damageGiveEvent = new DamageGiveEvent(stat.calculateTrueDamage(tags, coefficient),
                    Vector3.zero, player, null, tags, 2f);
            }
        }

        public override void skill()
        {
            if (!isActiveAndEnabled || !player || player.InputSuppressed || player.stat == null || player.stat.nowHp <= 0 || SkillStack <= 0 || !knifeSkill) return;
            if (SkillStack == Mathf.Max(1, (int)maxSkillStack)) skillTimer = skillDuration = RechargeDuration;
            SkillStack--;
            new SkillExecuteEvent(player, null).trigger();
            var stat = player.stat.calculate();
            foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                if (!enemy.isActiveAndEnabled || enemy.stat == null || enemy.stat.nowHp <= 0 ||
                    Vector2.Distance(player.transform.position, enemy.transform.position) > skillRadius) continue;
                Vector2 direction = (enemy.transform.position - player.transform.position).normalized;
                var instance = Instantiate(knifeSkill, player.transform.position, Quaternion.identity);
                instance.direction = direction;
                instance.rigidbody2D.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.skillDamage);
                instance.damageGiveEventHit = new DamageGiveEvent(stat.calculateTrueDamage(tags, 50),
                    Vector3.zero, player, null, tags, 3f);
                instance.marking = marking;
            }
        }

        public override void ultimate()
        {
            if (!isActiveAndEnabled || !player || player.InputSuppressed || player.stat == null || player.stat.nowHp <= 0 || Energy < requireEnergy) return;
            Energy -= requireEnergy;
            Vector2 center = aimSupport ? (Vector2)aimSupport.target : (Vector2)player.transform.position;
            marking.MakeOneHitInArea(center, ultimateRadius);
            new UltimateExecuteEvent(player, requireEnergy).trigger();
        }

        public override void eventActive(EventArgs eventArgs)
        {
            if (!player || player.stat == null) return;
            float gain = 0;
            if (eventArgs is DamageGiveEvent hit && hit.attacker == player) gain = hit.energeRecharge;
            else if (eventArgs is JustDodgeEvent) gain = 5;
            else if (eventArgs is DamageTakeEvent taken && taken.target == player) gain = 1;
            Energy = Mathf.Clamp(Energy + gain * player.stat.calculate().energyRecharge, 0, Mathf.Max(maxEnergy, requireEnergy));
        }

        public override void update(float deltaTime, Entity target)
        {
            if (target != player) return;
            fireTimer = Mathf.Max(0, fireTimer - deltaTime);
            if (SkillStack >= Mathf.Max(1, (int)maxSkillStack)) return;
            skillTimer -= deltaTime;
            while (skillTimer <= 0 && SkillStack < Mathf.Max(1, (int)maxSkillStack))
            {
                SkillStack++;
                skillDuration = RechargeDuration;
                skillTimer += skillDuration;
            }
        }

        private void Update()
        {
            if (!player || player.stat == null || player.stat.nowHp <= 0) return;
            transform.position = player.transform.position;
            Vector2 direction = (Vector2)aimSupport.target - (Vector2)player.transform.position;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (Input.GetKeyDown(KeyCode.E)) skill();
            if (Input.GetKeyDown(KeyCode.Q)) ultimate();
        }

        public override void WriteHud(WeaponHudState state)
        {
            base.WriteHud(state);
            state.Title = "건 / 나이프";
            state.Subtitle = "나이프로 표식 부여 · 사격으로 표식 폭발";
            state.Entries.Add(WeaponHudEntry.Skill("표식 투척", SkillStack, Mathf.Max(1, (int)maxSkillStack),
                skillTimer, skillDuration));
            state.Entries.Add(WeaponHudEntry.Ultimate("처형 구역", Energy, Mathf.Max(maxEnergy, requireEnergy), requireEnergy));
        }

        protected override void OnDisable()
        {
            if (marking && marking.player == player) marking.removeSelf();
            base.OnDisable();
        }
    }
}
