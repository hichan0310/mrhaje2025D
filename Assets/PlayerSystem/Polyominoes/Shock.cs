using System.Collections.Generic;
using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Polyominoes
{
    public class Shock : DamageOverTime
    {
        protected override float cooldown => 1.5f;

        private EntityStat attackerStat;
        private float lifeTime;
        private float tickCoefficient;

        private readonly AtkTagSet dotTags = new AtkTagSet().Add(
            AtkTags.damageOverTime,
            AtkTags.electricalDamage
        );

        // 마비 버프 중첩 방지용 전역 관리
        private class ParalysisState
        {
            public int refCount;
            public ParalysisTakenDamageUpBuff buff;
        }

        private static readonly Dictionary<Entity, ParalysisState> paralysisStates = new Dictionary<Entity, ParalysisState>();

        private bool appliedParalysis;
        private float paralysisPercent;

        // ElectricField에서 생성 직후 호출
        public void Init(EntityStat attackerStatSnapshot, Entity target, float coef, float power)
        {
            attackerStat = attackerStatSnapshot;

            // 지속시간 10초. 스탯 additionalDuration 반영하고 싶으면 아래처럼
            lifeTime = 10f * (attackerStatSnapshot?.additionalDuration ?? 0f);

            // DoT 크기 (명시 수치가 없어서 "초기 타격 (power+1)*50"보다 작은 값으로 기본 배치)
            // 원하면 여기만 바꾸면 됨.
            tickCoefficient = coef;

            // 마비 피해 증가 수치
            if (power > 2f)
            {
                paralysisPercent = 20;
                ApplyParalysisOnce(target, paralysisPercent);
                appliedParalysis = true;
            }

            registerTarget(target);
            
            var tag = new AtkTagSet(dotTags);
            var dmg = attackerStat.calculateTrueDamage(tag, tickCoefficient);
            this.damageGiveEvent=new DamageGiveEvent(dmg, Vector3.back, attackerStat.entity, target, tag, 0);
        }

        private void ApplyParalysisOnce(Entity t, float percent)
        {
            if (t == null || t.stat == null) return;

            if (!paralysisStates.TryGetValue(t, out var state))
            {
                state = new ParalysisState
                {
                    refCount = 0,
                    buff = new ParalysisTakenDamageUpBuff(percent)
                };
                paralysisStates[t] = state;

                // 처음 들어오는 Shock만 버프 등록
                t.stat.registerBuff(state.buff);
            }

            state.refCount++;
        }

        private void ReleaseParalysis(Entity t)
        {
            if (t == null || t.stat == null) return;
            if (!paralysisStates.TryGetValue(t, out var state)) return;

            state.refCount--;
            if (state.refCount <= 0)
            {
                // 마지막 Shock가 사라질 때 버프 제거
                t.stat.removeBuff(state.buff);
                paralysisStates.Remove(t);
            }
        }

        public override void removeSelf()
        {
            if (appliedParalysis)
            {
                ReleaseParalysis(target);
                appliedParalysis = false;
            }
            base.removeSelf();
        }

        private void OnDestroy()
        {
            // Destroy로 사라질 때도 정리
            if (target != null)
                removeSelf();
        }

        public override void update(float deltaTime, Entity target)
        {
            if (this.target != target) return;
            base.update(deltaTime, target);
            lifeTime -= deltaTime;

            // 만료
            if (lifeTime <= 0f)
            {
                removeSelf();
                Destroy(gameObject);
            }
        }
    }
    
    // 마비: 특정 태그 받는 피해 증가 (중첩 불가하게 Shock에서 1개만 등록)
    class ParalysisTakenDamageUpBuff : IBuff
    {
        public bool isStable => false;
        public void applyBuff(IStat stat)
        {
            if (stat is EntityStat entityStat)
            {
                entityStat.dmgTakeUp[(int)AtkTags.damageOverTime] += _percent;
                entityStat.dmgTakeUp[(int)AtkTags.combatModuleDamage] += _percent;
            }
        }

        public void removeSelf()
        {
            
        }

        private readonly float _percent;

        public ParalysisTakenDamageUpBuff(float percent)
        {
            _percent = percent;
        }
    }
}
