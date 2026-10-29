using System;
using DefaultNamespace;
using DG.Tweening;
using EntitySystem;
using EntitySystem.Events;
using GameBackend;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;
using Random = UnityEngine.Random;

namespace PlayerSystem.Weapons.Sniper
{
    public class Sniper : Weapon
    {
        public GameObject firePoint;
        public NormalBullet bulletNormalPrefab;
        public SkillBullet skillBulletPrefab;
        public GameObject muzzleFlashPrefab;
        public UltimateBullet ultimateBulletPrefab;
        private float energy = 0;
        private float skillCooldown = 0;
        private int skillStackCount = 0;
        private float skillCooltimeNow;
        [SerializeField] private float baseSkillCooltime;
        [SerializeField] private int maxSkillStack;
        [SerializeField] private int ultimateEnergyUsage;
        [SerializeField] private int maxUltimateEnergyCharge;

        private AtkTagSet atkTagSet = new AtkTagSet().Add(AtkTags.electricalDamage, AtkTags.normalAttackDamage);
        private AtkTagSet ultimateTag = new AtkTagSet().Add(AtkTags.electricalDamage, AtkTags.ultimateDamage);

        private bool skillBullet = false;

        public override void registerTarget(Entity target, object args = null)
        {
            base.registerTarget(target, args);
            skillStackCount = maxSkillStack;
            skillCooltimeNow = Mathf.Max(0.1f, baseSkillCooltime * 4f /
                Mathf.Max(0.1f, player.stat.calculate().skillCooldownDecrease + 4f));
            skillCooldown = skillCooltimeNow;
        }

        public override void fire()
        {
            if (!isActiveAndEnabled || !player || player.InputSuppressed || player.stat == null || player.stat.nowHp <= 0 || inUltimate) return;
            new BasicAttackExecuteEvent(player, aimSupport.target).trigger();
            if (!skillBullet)
            {
                Vector2 target = this.aimSupport.target;
                Vector2 fire = firePoint.transform.position;
                Vector2 direction = target - fire;
                direction.Normalize();
                var b = Instantiate(bulletNormalPrefab, fire, Quaternion.identity);
                b.rigidbody2D.position = fire;
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg +
                            this.aimSupport.aimRange * (Random.Range(-0.3f, 0.3f));
                b.rigidbody2D.rotation = angle;
                b.direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                var stat = this.player.stat.calculate();
                var coef = 100 * (stat.bulletRate + 1) * (stat.bulletSpeed + 1) * (stat.fireSpeed + 1) / 8;
                if (this.aimSupport is SniperAim s)
                {
                    coef *= Mathf.Min(0.5f + s.aimDuration, 1f) / 2;
                    if (s.aimDuration >= 0.3f)
                    {
                        stat.crit += 50;
                        coef *= 5;
                    }

                    var tag = new AtkTagSet(atkTagSet);
                    var dmg = stat.calculateTrueDamage(tag, coef);
                    b.damageGiveEvent = new DamageGiveEvent(dmg, Vector3.zero, player, null, tag,
                        3 * Mathf.Min(s.aimDuration, 1f));
                    Destroy(Instantiate(muzzleFlashPrefab, fire, Quaternion.identity), 1);
                }
            }
            else
            {
                Vector2 target = this.aimSupport.target;
                Vector2 fire = firePoint.transform.position;
                Vector2 direction = target - fire;
                direction.Normalize();
                var b = Instantiate(skillBulletPrefab, fire, Quaternion.identity);
                b.rigidbody2D.position = fire;
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg +
                            this.aimSupport.aimRange * (Random.Range(-0.3f, 0.3f));
                b.rigidbody2D.rotation = angle;
                b.direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                var stat = this.player.stat.calculate();
                var coef = 100 * (stat.bulletRate + 1) * (stat.bulletSpeed + 1) * (stat.fireSpeed + 1) / 8f;
                if (this.aimSupport is SniperAim s)
                {
                    coef *= Mathf.Min(0.5f + s.aimDuration, 1f) / 2;
                    if (s.aimDuration >= 0.3f)
                    {
                        stat.crit += 50;
                        coef *= 5;
                    }

                    coef = coef * 0.8f + 100;

                    var tag = new AtkTagSet().Add(AtkTags.electricalDamage, AtkTags.skillDamage);
                    var dmg = stat.calculateTrueDamage(tag, coef);
                    b.damageGiveEvent = new DamageGiveEvent(dmg, Vector3.zero, player, null, tag,
                        5 * Mathf.Min(s.aimDuration, 1f));
                    Destroy(Instantiate(muzzleFlashPrefab, fire, Quaternion.identity), 1);
                    skillBullet = false;
                }
            }
        }

        public override void skill()
        {
            if (!isActiveAndEnabled || !player || player.InputSuppressed || player.stat == null || player.stat.nowHp <= 0 || skillStackCount <= 0 ||
                skillBullet || inUltimate || aimSupport.isAiming) return;
            if (skillStackCount == maxSkillStack)
                skillCooldown = skillCooltimeNow = Mathf.Max(0.1f, baseSkillCooltime * 4f /
                    Mathf.Max(0.1f, player.stat.calculate().skillCooldownDecrease + 4f));
            skillStackCount--;
            new SkillExecuteEvent(player, null).trigger();
            this.skillBullet = true;
            this.player.TryDodge(true);
        }

        public bool inUltimate { get; set; } = false;

        public override void ultimate()
        {
            if (!isActiveAndEnabled || !player || player.InputSuppressed || player.stat == null || player.stat.nowHp <= 0 || inUltimate ||
                aimSupport.isAiming || energy < ultimateEnergyUsage) return;
            energy -= ultimateEnergyUsage;
            TimeScaler.Instance.changeTimeScale(1f / 3);
            inUltimate = true;
        }

        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent d && d.attacker == player)
            {
                this.energy += d.energeRecharge * player.statCache.energyRecharge;
                if (this.energy > this.maxUltimateEnergyCharge) this.energy = this.maxUltimateEnergyCharge;
            }
        }

        public override void update(float deltaTime, Entity target)
        {
        }

        private void releaseUltimate()
        {
            this.inUltimate = false;
        }

        private void Update()
        {
            if (!player || player.stat == null || player.stat.nowHp <= 0) return;
            if (skillStackCount < maxSkillStack)
            {
                this.skillCooldown -= TimeManager.deltaTime;
                if (this.skillCooldown <= 0)
                {
                    this.skillStackCount += 1;
                    this.skillCooldown += Mathf.Max(0.1f, baseSkillCooltime * 4f / Mathf.Max(0.1f, player.stat.calculate().skillCooldownDecrease + 4f));
                    this.skillCooltimeNow = Mathf.Max(0.1f, baseSkillCooltime * 4f / Mathf.Max(0.1f, player.stat.calculate().skillCooldownDecrease + 4f));
                }
            }

            Vector2 pp = player.transform.position;
            this.transform.position = pp;
            Vector2 t = this.aimSupport.target;
            Vector2 p = this.player.transform.position;
            if (t.x < p.x) this.transform.localScale = new Vector3(1, -1, 1);
            else this.transform.localScale = new Vector3(1, 1, 1);
            this.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(t.y - p.y, t.x - p.x) * Mathf.Rad2Deg);

            if (Input.GetKeyDown(KeyCode.E)) skill();
            if (Input.GetKeyDown(KeyCode.Q)) ultimate();

            if (inUltimate && Input.GetMouseButtonUp(0))
            {
                Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                TimeScaler.Instance.changeTimeScale(3f);
                new UltimateExecuteEvent(this.player, ultimateEnergyUsage).trigger();
                var stat = this.player.stat.calculate();
                Vector2 target = this.aimSupport.target;
                Vector2 fire = firePoint.transform.position;
                Vector2 direction = target - fire;
                float length = direction.magnitude;
                direction.Normalize();
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var bullets = (int)stat.bulletRate + 2;
                for (int i = 0; i < bullets; i++)
                {
                    var b = Instantiate(ultimateBulletPrefab, fire, Quaternion.identity);
                    b.rigidbody2D.position = fire;


                    float angleOffset;

                    var tmp = (bullets % 2 == 0) ? bullets * 2 + 1 : bullets * 2 - 1;
                    var tmpp = Mathf.Atan(Mathf.Sqrt(stat.skillRange) * bullets) / Mathf.PI * 4 * 15 / (tmp - 1) *
                               Mathf.Sqrt((float)bullets / 3);
                    // Debug.Log(tmp);
                    // Debug.Log(tmpp);
                    // Debug.Log(stat.skillRange);
                    if (bullets % 2 == 0)
                    {
                        var tmppp = i * 2 - bullets + 1;
                        angleOffset = tmppp * tmpp;
                    }
                    else
                    {
                        var tmppp = i * 2 - bullets + 1;
                        angleOffset = tmppp * tmpp;
                    }

                    var a = angle + angleOffset;
                    // Debug.Log(angleOffset); // 이거 왜 NaN임?
                    b.rigidbody2D.rotation = a;
                    var d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    b.pos = fire + d * (length * (1 + (Random.value - 0.5f) / 15));
                    b.rigidbody2D.linearVelocity = d * 40;

                    b.player = player;
                    b.ultimateTag = this.ultimateTag;
                    b.stat = player.stat.calculate();
                }

                Invoke("releaseUltimate", 0.01f);
                //Destroy(Instantiate(muzzleFlashPrefab, fire, Quaternion.identity), 1);
            }

        }

        protected override void OnDisable()
        {
            if (aimSupport is SniperAim aim) aim.ReleaseSlow();
            if (inUltimate)
            {
                if (TimeScaler.Instance) TimeScaler.Instance.changeTimeScale(3f);
                else Time.timeScale *= 3f;
            }
            inUltimate = false;
            base.OnDisable();
        }

        public override void WriteHud(WeaponHudState state)
        {
            base.WriteHud(state);
            state.Title = "저격총";
            state.Subtitle = "길게 조준해 정밀 사격 · E 회피 후 강화탄";
            string block = inUltimate ? "필살기 조준 중" : aimSupport && aimSupport.isAiming ? "조준 해제 후 사용" : "";
            state.Entries.Add(WeaponHudEntry.Skill("회피 · 강화탄", skillStackCount, maxSkillStack,
                skillCooldown, skillCooltimeNow, skillBullet ? "강화탄 발사 후 재사용" : block, skillBullet));
            state.Entries.Add(WeaponHudEntry.Ultimate("전격 포화", energy, maxUltimateEnergyCharge,
                ultimateEnergyUsage, inUltimate ? "좌클릭을 놓으면 발사" : block, inUltimate));
            if (aimSupport is SniperAim aim && aimSupport.isAiming && !inUltimate)
                state.Entries.Add(new WeaponHudEntry("", "정밀 조준", $"{aim.aimDuration:0.00}초",
                    aim.aimDuration >= 0.3f ? "정밀 사격 준비 완료" : "0.3초부터 정밀 사격",
                    aim.aimDuration / 0.5f, WeaponHudAccent.Effect, active: aim.aimDuration >= 0.3f, compact: true));
        }
    }
}
