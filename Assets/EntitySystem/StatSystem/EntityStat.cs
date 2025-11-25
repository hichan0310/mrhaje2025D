using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EntitySystem.StatSystem
{
    public class EntityStat : IStat
    {
        public Entity entity { get; set; }
        public int maxHp { get; set; }
        public int nowHp { get; set; }

        private int baseAtk { get; }
        public int addAtk { get; set; }
        public float increaseAtk { get; set; }
        public int atk => (int)(baseAtk * (increaseAtk / 100 + 1) + addAtk);

        private int baseDef { get; }
        public int addDef { get; set; }
        public float increaseDef { get; set; }
        public int def => (int)(baseDef * (increaseDef / 100 + 1) + addDef);

        public float crit { get; set; }
        public float critDmg { get; set; }

        public float[] dmgUp { get; set; }
        public float[] dmgAdd { get; set; }

        public float energyRecharge { get; set; }

        public float speed { get; set; }
        public int jumpCount { get; set; }
        public float jumpPower { get; set; }
        public float airAcceleration { get; set; }
        public float groundAcceleration { get; set; }
        public float dodgeSpeed { get; set; }
        public float dodgeTime { get; set; }
        public float dodgeCooldown { get; set; }

        public float skillCooldownDecrease { get; set; }
        public float bulletRate { get; set; }
        public float bulletSpeed { get; set; }
        public float additionalDuration { get; set; }
        public float skillRange { get; set; }
        public float fireSpeed { get; set; }

        public enum ArmorType
        {
            Normal,
            SpecialArmor,
            HeavyArmor
        }

        public EntityStat(Entity entity, int hp, int baseAtk, int baseDef)
        {
            this.entity = entity;
            maxHp = Mathf.Max(1, hp);
            nowHp = maxHp;

            this.baseAtk = Mathf.Max(0, baseAtk);
            this.baseDef = Mathf.Max(0, baseDef);
            addAtk = 0;
            increaseAtk = 0f;
            addDef = 0;
            increaseDef = 0f;

            crit = 0f;
            critDmg = 50f;

            dmgUp = new float[Tag.atkTagCount];
            dmgAdd = new float[Tag.atkTagCount];

            energyRecharge = 1f;
            speed = 8f;
            jumpCount = 1;
            jumpPower = 10f;
            airAcceleration = 20f;
            groundAcceleration = 20f;

            dodgeSpeed = 15f;
            dodgeTime = 0.3f;
            dodgeCooldown = 0.7f;


            skillCooldownDecrease = 1f;
            bulletRate = 1f;
            bulletSpeed = 1f;
            additionalDuration = 0.2f;
            skillRange = 0.2f;
            fireSpeed = 1;
        }

        public EntityStat(EntityStat copy)
        {
            this.entity = copy.entity;
            this.maxHp = copy.maxHp;
            this.nowHp = copy.nowHp;
            this.baseAtk = copy.baseAtk;
            this.addAtk = copy.addAtk;
            this.increaseAtk = copy.increaseAtk;
            this.baseDef = copy.baseDef;
            this.addDef = copy.addDef;
            this.increaseDef = copy.increaseDef;
            this.crit = copy.crit;
            this.critDmg = copy.critDmg;

            this.dmgUp = new float[Tag.atkTagCount];
            this.dmgAdd = new float[Tag.atkTagCount];
            Array.Copy(copy.dmgUp, this.dmgUp, Tag.atkTagCount);
            Array.Copy(copy.dmgAdd, this.dmgAdd, Tag.atkTagCount);

            this.energyRecharge = copy.energyRecharge;
            this.speed = copy.speed;
            this.jumpCount = copy.jumpCount;
            this.jumpPower = copy.jumpPower;
            this.airAcceleration = copy.airAcceleration;
            this.groundAcceleration = copy.groundAcceleration;

            this.dodgeSpeed = copy.dodgeSpeed;
            this.dodgeTime = copy.dodgeTime;
            this.dodgeCooldown = copy.dodgeCooldown;


            this.skillCooldownDecrease = copy.skillCooldownDecrease;
            this.bulletRate = copy.bulletRate;
            this.bulletSpeed = copy.bulletSpeed;
            this.additionalDuration = copy.additionalDuration;
            this.skillRange = copy.skillRange;
            this.fireSpeed = copy.fireSpeed;
        }

        // 모든 버프에는 교환법칙이 성립한다고 가정
        private List<IBuff> changeBuffs = new List<IBuff>();
        private List<IBuff> stableBuffs = new List<IBuff>();

        public void registerBuff(IBuff buff)
        {
            if (buff.isStable)
            {
                stableBuffs.Add(buff);
            }
            else
            {
                changeBuffs.Add(buff);
            }
        }

        public void removeBuff(IBuff buff)
        {
            if (buff.isStable)
            {
                stableBuffs.Remove(buff);
            }
            else
            {
                changeBuffs.Remove(buff);
            }
        }

        public int calculateTrueDamage(AtkTagSet tags, float coefficient)
        {
            if (changeBuffs.Count > 0) return calculate().calculateTrueDamage(tags, coefficient);

            float dmg = coefficient * this.atk / 100;
            dmg += dmgAdd[(int)AtkTags.all];
            var tagSet = tags ?? AtkTagSet.None;
            foreach (AtkTags atkTag in tagSet)
            {
                if (atkTag == AtkTags.all) continue;
                dmg += dmgAdd[(int)atkTag];
            }
            if (Random.value < crit / 100 && !tagSet.Contains(AtkTags.notcriticalHit))
            {
                dmg = (int)(dmg * (1 + critDmg / 100));
                tagSet.Add(AtkTags.criticalHit);
            }
            else if (tagSet.Contains(AtkTags.criticalHit))
            {
                dmg = (int)(dmg * (1 + critDmg / 100));
            }

            float dmgUpSum = dmgUp[(int)AtkTags.all];
            foreach (AtkTags atkTag in tagSet)
            {
                if (atkTag == AtkTags.all) continue;
                dmgUpSum += dmgUp[(int)atkTag];
            }

            dmg = (int)((dmgUpSum / 100 + 1) * dmg);
            return (int)dmg;
        }

        public int calculateTakenDamage(AtkTagSet tags, int damage)
        {
            if (changeBuffs.Count > 0) return calculate().calculateTakenDamage(tags, damage);

            int C = 200;

            return (int)(damage * ((float)(C) / (def + C)));
        }

        public void takeDamage(int damage)
        {
            nowHp = Mathf.Max(0, nowHp - damage);
        }

        public virtual EntityStat calculate()
        {
            var newStat = new EntityStat(this);

            foreach (var buff in changeBuffs)
            {
                buff.applyBuff(newStat);
            }

            return newStat;
        }

        public class EnemyStat : EntityStat
        {
            /// <summary>
            /// 방어 타입 (예: 특수장갑)
            /// </summary>
            public ArmorType armorType { get; set; }

            /// <summary>
            /// 넉백 저항 (0 = 그대로, 1 = 완전 무시)
            /// </summary>
            public float knockbackResist { get; set; }

            /// <summary>
            /// 몸통박치기/접촉 피해 배율 같은 거에 쓰고 싶으면 사용
            /// </summary>
            public float contactDamageMultiplier { get; set; }

            // 기본 생성자: Enemy용으로 새 스탯 초기화할 때 사용
            public EnemyStat(Entity entity,
                             int hp,
                             int baseAtk,
                             int baseDef,
                             ArmorType armorType = ArmorType.Normal,
                             float knockbackResist = 0f,
                             float contactDamageMultiplier = 1f)
                : base(entity, hp, baseAtk, baseDef)
            {
                this.armorType = armorType;
                this.knockbackResist = knockbackResist;
                this.contactDamageMultiplier = contactDamageMultiplier;
            }

            // 복사 생성자: 필요하면 쓸 수 있음 (지금은 컨트롤러에서 안 써도 됨)
            public EnemyStat(EnemyStat copy)
                : base(copy)
            {
                this.armorType = copy.armorType;
                this.knockbackResist = copy.knockbackResist;
                this.contactDamageMultiplier = copy.contactDamageMultiplier;
            }
        }
    }
}