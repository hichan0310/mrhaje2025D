using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons.GunAndKnife
{
    public class GunAndKnife : Weapon
    {
        public float fireCooldownNormal;
        public float fireCooldownUltimate;
        public float skillCooldown;
        public float maxSkillStack;
        public float ultimateDuration;
        public int requireEnergy;
        public int maxEnergy;

        private float fireTimer;
        private float skillTimer = 0;
        private int skillStack;
        private bool isUltimate;
        private float ultimateTimer = 0;
        private float energy = 0;

        [SerializeField] private Bullet bullet;
        [SerializeField] private KnifeSkill knifeSkill;
        [SerializeField] private GameObject firePoint;


        public GameObject muzzleFlashPrefab;
        public Mark marking;


        [SerializeField] private int bulletNumAdd = 0;

        private AtkTagSet atkTagSetBullet = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);


        public override void registerTarget(Entity target, object args = null)
        {
            base.registerTarget(target, args);
            marking.player=player;
        }

        public override void fire()
        {
            if (fireTimer <= 0)
            {
                var stat = this.player.stat.calculate();
                Vector2 target = this.aimSupport.target;
                Vector2 fire = firePoint.transform.position;
                Vector2 direction = target - fire;
                direction.Normalize();
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var bullets = (int)stat.bulletRate + bulletNumAdd;
                for (int i = 0; i < bullets; i++)
                {
                    var b = Instantiate(bullet, fire, Quaternion.identity);
                    b.rigidbody2D.position = fire;


                    float angleOffset;

                    var tmp = (bullets % 2 == 0) ? bullets * 2 + 1 : bullets * 2 - 1;
                    var tmpp = this.aimSupport.aimRange / (tmp - 1);
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
                    //Debug.Log(angleOffset);
                    b.rigidbody2D.rotation = a;
                    b.direction = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));


                    var coef = 200f/((int)(stat.bulletRate)+3);

                    var tag = new AtkTagSet(atkTagSetBullet);
                    var dmg = stat.calculateTrueDamage(tag, coef);
                    b.damageGiveEvent = new DamageGiveEvent(dmg, Vector3.zero, player, null, tag, 10);
                }
                //Destroy(Instantiate(muzzleFlashPrefab, fire, Quaternion.identity), 1);
                this.fireTimer = fireCooldownNormal;
            }
        }

        public override void skill()
        {
            if (isUltimate)
            {
            }
            else if (skillStack > 0)
            {
                var stat = player.stat.calculate();
                fireTimer = fireCooldownNormal;
                Entity[] allEntities = FindObjectsByType<Entity>(0);

                Vector3 myPos = transform.position;

                foreach (var e in allEntities)
                {
                    if (!e) continue;

                    // 자기 자신이면 스킵 (원하면 제거)
                    if (e.gameObject == this.player.gameObject)
                        continue;

                    float dist = Vector3.Distance(myPos, e.transform.position);
                    if (dist <= 10)
                    {
                        var b = Instantiate(knifeSkill, player.transform.position, Quaternion.identity);
                        b.rigidbody2D.position = player.transform.position;

                
                        Vector2 target = e.transform.position;
                        Vector2 fire = player.transform.position;
                        Vector2 direction = target - fire;
                        direction.Normalize();
                        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                
                        b.rigidbody2D.rotation = angle;
                        b.direction = direction;

                        var coef = 50;

                        var tag = new AtkTagSet(atkTagSetBullet);
                        var dmg = stat.calculateTrueDamage(tag, coef);
                        b.damageGiveEventHit=new DamageGiveEvent(dmg, Vector3.zero, player, null, tag, 5);
                        b.marking=this.marking;
                    }
                }
                
                
                
                
                
                
                
                
                
            }
        }

        public override void ultimate()
        {
            if (energy >= requireEnergy)
            {
            }
        }

        public override void eventActive(EventArgs eventArgs)
        {
            switch (eventArgs)
            {
                case DamageGiveEvent damageGiveEvent:
                {
                    this.energy += player.statCache.energyRecharge * damageGiveEvent.energeRecharge;
                    break;
                }
                case JustDodgeEvent justDodgeEvent:
                {
                    this.energy += player.statCache.energyRecharge * 5;
                    break;
                }
                case DamageTakeEvent damageTakeEvent:
                {
                    this.energy += player.statCache.energyRecharge * 1;
                    break;
                }
            }
        }

        public override void update(float deltaTime, Entity target)
        {
            fireTimer -= deltaTime;
            if (fireTimer < 0) fireTimer = 0;

            if (skillStack < maxSkillStack)
            {
                skillTimer -= deltaTime;
                if (skillTimer < 0)
                {
                    skillTimer += skillCooldown;
                    skillStack += 1;
                }
            }

            ultimateTimer -= deltaTime;
            if (ultimateTimer < 0) ultimateTimer = 0;
        }

        private void Update()
        {
            Vector2 pp = player.transform.position;
            this.transform.position = pp;
            Vector2 t = this.aimSupport.target;
            Vector2 p = this.player.transform.position;
            if (t.x < p.x) this.transform.localScale = new Vector3(1, -1, 1);
            else this.transform.localScale = new Vector3(1, 1, 1);
            this.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(t.y - p.y, t.x - p.x) * Mathf.Rad2Deg);

            if (Input.GetKeyDown(KeyCode.E))
            {
                skill();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                ultimate();
            }
        }
    }
}