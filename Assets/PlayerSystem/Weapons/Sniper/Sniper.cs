using System;
using DefaultNamespace;
using DG.Tweening;
using EntitySystem;
using EntitySystem.Events;
using GameBackend;
using TMPro;
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

        public override void fire()
        {
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

                    var tag = new AtkTagSet(atkTagSet);
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
            this.skillBullet = true;
            this.player.TryDodge(true);
        }

        public bool inUltimate { get; set; } = false;

        public override void ultimate()
        {
            TimeScaler.Instance.changeTimeScale(1f / 3);
            inUltimate = true;
        }

        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent d)
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
            if (skillStackCount < maxSkillStack)
            {
                this.skillCooldown -= TimeManager.deltaTime;
                if (this.skillCooldown <= 0)
                {
                    this.skillStackCount += 1;
                    this.skillCooldown += baseSkillCooltime * (4 / (player.statCache.skillCooldownDecrease + 4));
                    this.skillCooltimeNow = baseSkillCooltime * (4 / (player.statCache.skillCooldownDecrease + 4));
                    if (this.skillCooltimeNow >= this.baseSkillCooltime)
                    {
                        skillCooldown = 0;
                    }
                }
            }

            Vector2 pp = player.transform.position;
            this.transform.position = pp;
            Vector2 t = this.aimSupport.target;
            Vector2 p = this.player.transform.position;
            if (t.x < p.x) this.transform.localScale = new Vector3(1, -1, 1);
            else this.transform.localScale = new Vector3(1, 1, 1);
            this.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(t.y - p.y, t.x - p.x) * Mathf.Rad2Deg);

            if (!aimSupport.isAiming && !inUltimate && Input.GetKeyDown(KeyCode.E))
            {
                if (skillStackCount > 0)
                {
                    if (this.skillStackCount == maxSkillStack)
                    {
                        this.skillCooldown = baseSkillCooltime * (4 / (player.statCache.skillCooldownDecrease + 4));
                    }

                    this.skillStackCount -= 1;
                    skill();
                }
            }

            if (!aimSupport.isAiming && !inUltimate && Input.GetKeyDown(KeyCode.Q))
            {
                if (energy >= ultimateEnergyUsage)
                {
                    energy -= ultimateEnergyUsage;
                    ultimate();
                }
            }

            if (inUltimate && Input.GetMouseButtonUp(0))
            {
                Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                TimeScaler.Instance.changeTimeScale(3f);
                new UltimateExecuteEvent(this.player, 50).trigger();
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

            uiControl();
        }

        [SerializeField] private TMP_Text skillCountText;
        [SerializeField] private TMP_Text ultimateEnergyText;
        [SerializeField] private RectTransform skillCooldownProgressBar;
        [SerializeField] private RectTransform ultimateEnergyProgressBar;

        private void uiControl()
        {
            skillCountText.text = skillStackCount.ToString();
            ultimateEnergyText.text = $"{(int)energy}/{maxUltimateEnergyCharge}";

            skillCooldownProgressBar.localScale =
                new Vector3((skillCooltimeNow - skillCooldown) / skillCooltimeNow, 1, 1);
            ultimateEnergyProgressBar.localScale = new Vector3(energy / maxUltimateEnergyCharge, 1, 1);
        }
    }
}