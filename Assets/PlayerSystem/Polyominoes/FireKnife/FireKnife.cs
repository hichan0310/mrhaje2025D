using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Polyominoes.FireKnife
{
    public class FireKnife : Polyomino, IEntityEventListener
    {
        public override string Name => "FireKnife";
        private float timer = 0;
        private float powerRecord = 0;
        private Entity target;

        private AtkTagSet atkTagSet = new AtkTagSet().Add(
            AtkTags.heatDamage, AtkTags.damageOverTime, AtkTags.notcriticalHit);

        private Dictionary<Entity, float> ICD = new Dictionary<Entity, float>();

        [SerializeField] private Burning burning;

        public override string Description =>
            "10초동안 일반 공격, 스킬, 필살기 피해를 가하면 최대 0.5초에 1번 화염 효과를 부여한다. \n" +
            "화염 효과가 부여된 적은 2초마다 1회 총 8초동안 열 속성의 지속 피해를 입힌다. 이 공격은 치명타가 들어가지 않는다. \n" +
            "화염 효과의 피해량은 공격력과 파워 수치에 비례하고 자기 자신에게는 화염 효과를 추가할 수 없다. \n" +
            "파워 수치가 들어오면 이전에 들어온 파워는 지속시간이 남아있어도 초기화된다. \n" +
            "화염 효과를 부여하는 상태와 화염 효과의 지속시간은 지속시간 추가를 통해 상승시킬 수 있다. \n" +
            this.statBonusDescription;

        public override void trigger(Entity player, float power)
        {
            timer = 10;
            if (player is Player p) timer += p.statCache.additionalDuration;
            this.registerTarget(player);
            this.powerRecord = power;
            // Debug.Log(power);
        }

        public void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent damageGiveEvent)
            {
                if (damageGiveEvent.attacker != this.target) return;
                if (damageGiveEvent.target == this.target) return;
                if (ICD.ContainsKey(damageGiveEvent.target)) return;
                if (damageGiveEvent.atkTags.Contains(
                        AtkTags.normalAttackDamage,
                        AtkTags.skillDamage,
                        AtkTags.ultimateDamage))
                {
                    var b = Instantiate(burning);
                    var stat = damageGiveEvent.attacker.stat.calculate();
                    var damage = stat.calculateTrueDamage(this.atkTagSet, (powerRecord + 1) * 20);
                    b.duration = 8 + stat.additionalDuration;
                    b.damageGiveEvent = new DamageGiveEvent(damage, Vector3.zero, target, damageGiveEvent.target,
                        this.atkTagSet, 0);
                    b.registerTarget(damageGiveEvent.target);
                    ICD.Add(damageGiveEvent.target, 0.5f);
                }
            }
        }

        public void registerTarget(Entity target, object args = null)
        {
            if (this.target) removeSelf();
            this.target = target;
            target.registerListener(this);
        }

        public void removeSelf()
        {
            target.removeListener(this);
            this.target = null;
        }

        private readonly List<Entity> _rm = new(64);
        private readonly List<Entity> _updK = new(64);
        private readonly List<float> _updV = new(64);

        public void update(float deltaTime, Entity target)
        {
            if (this.target != target) return;

            timer -= deltaTime;
            if (timer <= 0f)
            {
                timer = 0f;
                removeSelf();
                this.target = null;

                ICD.Clear();
                _rm.Clear();
                _updK.Clear();
                _updV.Clear();
                return;
            }

            _rm.Clear();
            _updK.Clear();
            _updV.Clear();

            foreach (var kv in ICD) // 여기서는 절대 ICD 수정 금지
            {
                float t = kv.Value - deltaTime;
                if (t <= 0f) _rm.Add(kv.Key);
                else
                {
                    _updK.Add(kv.Key);
                    _updV.Add(t);
                }
            }

            for (int i = 0; i < _updK.Count; i++) ICD[_updK[i]] = _updV[i];
            for (int i = 0; i < _rm.Count; i++) ICD.Remove(_rm[i]);
        }
    }
}