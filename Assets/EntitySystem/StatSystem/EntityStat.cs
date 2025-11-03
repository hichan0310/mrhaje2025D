using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EntitySystem.StatSystem
{
    public class EntityStat:IStat
    {
        public Entity entity{get;set;}
        public int maxHp { get; set; }
        public int nowHp { get; protected set; }

        private int baseAtk { get; }
        public int addAtk { get; set; }
        public float increaseAtk { get; set; }
        private int atk => (int)(baseAtk * (increaseAtk / 100 + 1) + addAtk);

        private int baseDef { get; }
        public int addDef { get; set; }
        public float increaseDef { get; set; }
        private int def => (int)(baseDef * (increaseDef / 100 + 1) + addDef);
        
        public float projectileSpeed { get; set; }
        public float projectileAmount { get; set; }
        public float projectilecoolTime { get; set; }
        public float projectileGuidence { get; set; }

        public float crit { get; set; }
        public float critDmg { get; set; }
        
        public float[] dmgUp { get; set; }
        public float[] dmgAdd { get; set; }
        
        public float energyRecharge { get; set; }
        public int energy { get; set; }
        
        public float speed { get; set; }
        public int jumpCount { get; set; }
        public float jumpPower { get; set; }
        public float airSpeed { get; set; }
        public float dodgeLength { get; set; }
        public float dodgeTime { get; set; }
        public float skillCooldownDecrease { get; set; }
        
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
            energy = 0;
            speed = 1;
            jumpCount = 1;
            jumpPower = 10f;
            airSpeed = 10f;
            dodgeLength = 1f;
            dodgeTime = 0.3f;
            
            projectileSpeed = 10f;
            projectileAmount = 1f;
            projectilecoolTime = 5f;
            projectileGuidence = 0f;
            skillCooldownDecrease = 1f;
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
            this.energy = copy.energy;
            this.speed = copy.speed;
            this.jumpCount = copy.jumpCount;
            this.jumpPower = copy.jumpPower;
            this.airSpeed = copy.airSpeed;
            this.dodgeLength = copy.dodgeLength;
            this.dodgeTime = copy.dodgeTime;
            
            this.projectileSpeed = copy.projectileSpeed;
            this.projectileAmount = copy.projectileAmount;
            this.projectilecoolTime = copy.projectilecoolTime;
            this.projectileGuidence = copy.projectileGuidence;
            this.skillCooldownDecrease = copy.skillCooldownDecrease;
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

            float dmg = coefficient*this.atk/100;
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
            if(changeBuffs.Count > 0) return calculate().calculateTakenDamage(tags, damage);

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
    }
}