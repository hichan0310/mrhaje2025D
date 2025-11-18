using System;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Weapons.Sniper
{
    public class Sniper:Weapon
    {
        public GameObject firePoint;
        public NormalBullet bulletNormalPrefab;
        public SkillBullet skillBulletPrefab;
        public GameObject muzzleFlashPrefab;
        
        private AtkTagSet atkTagSet=new AtkTagSet().Add(AtkTags.electricalDamage, AtkTags.normalAttackDamage);
        
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
                b.rigidbody2D.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                b.direction = direction;

                var stat = this.player.stat.calculate();
                var coef = 100 * (stat.bulletRate + 2) * (stat.bulletSpeed + 2) / 9;
                if (this.aimSupport is SniperAim s)
                {
                    coef *= Mathf.Min(0.5f + s.aimDuration, 1f) / 2;
                    if (s.aimDuration >= 0.3f)
                    {
                        stat.crit += 50;
                        coef *= 5;
                    }
                }

                var tag = new AtkTagSet(atkTagSet);
                var dmg = stat.calculateTrueDamage(tag, coef);
                b.damageGiveEvent = new DamageGiveEvent(dmg, Vector3.zero, player, null, tag, 10);
                Destroy(Instantiate(muzzleFlashPrefab, fire, Quaternion.identity), 1);
            }
            else
            {
                Vector2 target = this.aimSupport.target;
                Vector2 fire = firePoint.transform.position;
                Vector2 direction = target - fire;
                direction.Normalize();
                var b = Instantiate(skillBulletPrefab, fire, Quaternion.identity);
                b.rigidbody2D.position = fire;
                b.rigidbody2D.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                b.direction = direction;

                var stat = this.player.stat.calculate();
                var coef = 100 * (stat.bulletRate + 2) * (stat.bulletSpeed + 2) / 9;
                if (this.aimSupport is SniperAim s)
                {
                    coef *= Mathf.Min(0.5f + s.aimDuration, 1f) / 2;
                    if (s.aimDuration >= 0.3f)
                    {
                        stat.crit += 50;
                        coef *= 5;
                    }
                }
                coef=coef*0.8f+100;

                var tag = new AtkTagSet(atkTagSet);
                var dmg = stat.calculateTrueDamage(tag, coef);
                b.damageGiveEvent = new DamageGiveEvent(dmg, Vector3.zero, player, null, tag, 10);
                Destroy(Instantiate(muzzleFlashPrefab, fire, Quaternion.identity), 1);
                skillBullet = false;
            }
        }

        public override void skill()
        {
            this.skillBullet = true;
        }

        public override void ultimate()
        {
            Debug.Log("ultimate");
        }

        public override void eventActive(EventArgs eventArgs)
        {
            
        }

        public override void update(float deltaTime, Entity target)
        {
            
        }

        private void Update()
        {
            Vector2 pp = player.transform.position;
            this.transform.position = pp;
            Vector2 t=this.aimSupport.target;
            Vector2 p=this.player.transform.position;
            if (t.x < p.x) this.transform.localScale = new Vector3(1, -1, 1);
            else this.transform.localScale = new Vector3(1, 1, 1);
            this.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(t.y-p.y, t.x-p.x) * Mathf.Rad2Deg);
            
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