using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Skills.ElectricShock
{
    public class ElectricShock: Skill
    {
        private float cooldown = 0;
        private float cooldownDecreaseCooldown = 0f;

        private Shock shock = new Shock();

        public ElectricBomb bomb;
        
        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent && cooldownDecreaseCooldown <= 0)
            {
                cooldownDecreaseCooldown = 0.1f;
                cooldown -= 0.5f;
            }
        }

        public override void update(float deltaTime, Entity entity)
        {
            if (this.player == entity)
            {
                cooldown -= deltaTime;
                cooldownDecreaseCooldown -= deltaTime;
                
                if(cooldown < 0) cooldown = 0;
                if(cooldownDecreaseCooldown < 0) cooldownDecreaseCooldown = 0;
            }
        }

        public override string skillName=>"Electric Shock";

        public override string description =>
            "전방에 전기 수류탄을 던져서 피해를 입히고 마비 상태를 부여한다. " +
            "쿨타임은 20초이고 피해를 입히면 쿨타임이 0.5초씩 감소한다. 이것은 0.1초에 1번 일어날 수 있다. " +
            "마비 상태는 방어력 20% 감소 효과가 있으며 10초동안 지속되고 중첩되지 않는다.";
        public override float timeleft
        {
            get => cooldown; 
            set => cooldown = value;
        }
        public override void execute()
        {
            if(cooldown > 0.1f) return; // 선입력
            Invoke("realExecute", cooldown);
        }

        private void realExecute()
        {
            var stat=this.player.stat.calculate();
            for (int i = 0; i < 4; i++)
            {
                var b = Instantiate(bomb);
                b.transform.position = this.player.transform.position;
                b.angle = Mathf.PI / 5 * (1 + i);
                b.velocity = (i == 0 || i == 3) ? 6 : 4.5f;
                b.stat = stat;
                b.shock = shock;
            }
            new SkillExecuteEvent(this.player, this).trigger();
            this.cooldown = 20*this.player.stat.skillCooldownDecrease;
        }
    }

    class Shock:BuffOnce
    {
        public override bool isStable => true;

        public override void applyBuff(IStat stat) { }  // stable 버프여서 비워놔도 됩니다. 
        
        private class Time:IHaveTime
        {
            public float time => 10;
        }
        private Time time = new Time();

        public override void registerTarget(Entity target, object args = null)
        {
            if (!targets.ContainsKey(target))
            {
                // Debug.Log("apply");
                target.stat.increaseDef -= 20;
            }

            base.registerTarget(target, time);
        }

        protected override void removeTarget(Entity target)
        {
            if (targets.ContainsKey(target))
            {
                // Debug.Log("remove");
                target.stat.increaseDef += 20;
                base.removeTarget(target);
            }
        }
    }
}