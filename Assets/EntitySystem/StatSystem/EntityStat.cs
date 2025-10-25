using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EntitySystem.StatSystem
{
    public class EntityStat:IStat
    {
        public float speed { get; set; } = 1;
        public Entity entity{get;set;}
        private int baseHp { get; }
        public int addHp { get; set; }
        public float increaseHp { get; set; }

        public int maxHp
        {
            get => (int)(baseHp * (increaseHp / 100 + 1) + addHp);
        }

        public int nowHp { get; protected set; }

        private int baseAtk { get; }
        public int addAtk { get; set; }
        public float increaseAtk { get; set; }

        private int atk => (int)(baseAtk * (increaseAtk / 100 + 1) + addAtk);

        private int baseDef { get; }
        public int addDef { get; set; }
        public float increaseDef { get; set; }

        private int def => (int)(baseDef * (increaseDef / 100 + 1) + addDef);

        public float crit { get; set; }
        public float critDmg { get; set; }
        public float[] dmgUp { get; set; }
        public float movePower { get; set; }
        public float energyRecharge { get; set; }

        // 사실 배열 많이 쓰면 이거 복사할 때 무리가 갈 가능성도 있긴 해서 피증 하나만 하려고 했는데 2d면 딱히 상관 없으려나?
        public float[] dmgDrain { get; set; }
        public float[] dmgTakeUp { get; set; }
        public float[] dmgAdd { get; set; }
        
        public EntityStat(int baseHp, int baseAtk, int baseDef)
        {
            this.baseHp = Mathf.Max(1, baseHp);
            this.baseAtk = Mathf.Max(0, baseAtk);
            this.baseDef = Mathf.Max(0, baseDef);
            addHp = 0;
            increaseHp = 0f;
            addAtk = 0;
            increaseAtk = 0f;
            addDef = 0;
            increaseDef = 0f;
            crit = 0f;
            critDmg = 50f;
            dmgUp = new float[Tag.atkTagCount];
            dmgTakeUp = new float[Tag.atkTagCount];
            dmgDrain = new float[Tag.atkTagCount];
            dmgAdd = new float[Tag.atkTagCount];
            for (int i = 0; i < dmgDrain.Length; i++)
            {
                dmgDrain[i] = 1f;
            }

            movePower = 0f;
            energyRecharge = 0f;
            nowHp = maxHp;
        }

        public EntityStat(EntityStat copy)
        {
            this.speed = copy.speed;
            this.entity = copy.entity;
            this.baseHp = copy.baseHp;
            this.addHp = copy.addHp;
            this.increaseHp = copy.increaseHp;
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
            this.dmgTakeUp = new float[Tag.atkTagCount];
            this.dmgDrain = new float[Tag.atkTagCount];
            this.dmgAdd = new float[Tag.atkTagCount];

            this.movePower = copy.movePower;
            this.energyRecharge = copy.energyRecharge;
            Array.Copy(copy.dmgUp, this.dmgUp, Tag.atkTagCount);
            Array.Copy(copy.dmgTakeUp, this.dmgTakeUp, Tag.atkTagCount);
            Array.Copy(copy.dmgAdd, this.dmgAdd, Tag.atkTagCount);
            Array.Copy(copy.dmgDrain, this.dmgDrain, Tag.atkTagCount);
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

            float dmg = coefficient;
            dmg += dmgAdd[(int)AtkTags.all];
            foreach (AtkTags atkTag in tags)
            {
                if (atkTag == AtkTags.all) continue;
                dmg += dmgAdd[(int)atkTag];
            }
            if (Random.value < crit / 100 && !tags.Contains(AtkTags.notcriticalHit))
            {
                dmg = (int)(dmg * (1 + critDmg / 100));
                tags.Add(AtkTags.criticalHit);
            }
            else if (tags.Contains(AtkTags.criticalHit))
            {
                dmg = (int)(dmg * (1 + critDmg / 100));
            }

            float dmgUpSum = dmgUp[(int)AtkTags.all];
            dmg = (int)(dmgDrain[(int)AtkTags.all] * dmg);
            foreach (AtkTags atkTag in tags)
            {
                if (atkTag == AtkTags.all) continue;
                dmgUpSum += dmgUp[(int)atkTag];
                dmg = (int)(dmgDrain[(int)atkTag] * dmg);
            }

            dmg = (int)((dmgUpSum / 100 + 1) * dmg);
            return (int)dmg;
        }

        public int calculateTakenDamage(AtkTagSet tags, int damage)
        {
            if(changeBuffs.Count > 0) return calculate().calculateTakenDamage(tags, damage);
            
            int C = 200;
            float dmgUpSum = dmgTakeUp[(int)AtkTags.all];
            foreach (AtkTags atkTag in tags)
            {
                dmgUpSum += dmgTakeUp[(int)atkTag];
            }

            return (int)(damage * ((float)(C) / (def + C)) * (dmgUpSum / 100 + 1));
        }

        public void takeDamage(int damage)
        {
            nowHp -= damage;
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