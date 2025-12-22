using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using PlayerSystem.Tiling;

namespace PlayerSystem.Polyominoes.OverclockUnlocker
{
    public class OverclockUnlocker:Polyomino
    {
        public override string Name => "OverclockUnlocker";
        
        private Entity player;

        public override string Description =>
            "현재 hp의 20%를 소모하여 20초동안 공격력을 power*20%만큼 증가시킨다. \n" +
            "현재 hp가 최대 hp의 10% 미만일 경우 발동되지 않는다. 스택은 독립적으로 계산되고 체력만 충분하다면 무한히 중첩이 가능하다. \n" +
            this.statBonusDescription;
        
        public override void trigger(Entity player, float power)
        {
            if(player.stat.nowHp*10<player.stat.maxHp) return;
            if (player is Player p)
            {
                p.stat.nowHp = (int)(0.8f * p.stat.nowHp);
                new AtkUpStack(p.statCache.additionalDuration + 20, power).registerTarget(p);
            }
        }
        
        private class AtkUpStack:IEntityEventListener
        {
            private Entity target;

            private float timer;
            private float power;

            public AtkUpStack(float timer, float power)
            {
                this.timer = timer;
                this.power = power;
            }
            
            public void eventActive(EventArgs eventArgs)
            {
                
            }

            public void registerTarget(Entity target, object args = null)
            {
                this.target = target;
                this.target.registerListener(this);
                this.target.stat.increaseAtk += power * 20;
            }

            public void removeSelf()
            {
                this.target.removeListener(this);
                this.target = null;
            }

            public void update(float deltaTime, Entity target)
            {
                if(this.target!=target) return;
                this.timer -= deltaTime;
                if (timer < 0)
                {
                    removeSelf();
                    target.stat.increaseAtk-=power * 20;
                }
            }
        }
    }
}