using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Triggers
{
    // 간단히 내부쿨 0.5초로 피해를 주면 파워 1f의 트리거를 전체에 발동하게 만들어봄
    public class SimpleTriggerExample : Trigger
    {
        private float timer;
        public Entity target;

        

        private void Awake()
        {
            this.board = new Board(8, 6);
        }

        public bool tryAddEffect<T>(T effect, int stateIndex, int ax, int ay, out Board.Placement placement) where T : Polyomino, ITriggerEffect
        {
            var res=this.board.TryPlace(effect, stateIndex, ax, ay, out placement);
            return res;
        }

        public bool tryRemoveEffect<T>(int ax, int ay, out T effect) where T : Polyomino, ITriggerEffect
        {
            if (board.TryGetPlacementAt(ax, ay, out var placement))
            {
                var poly = board.getPolyomino(in placement);
                if (poly is ITriggerEffect t)
                {
                    effect = (T)t;
                    return true;
                }
            }
            effect = null;
            return false;
        }


        public override void eventActive(EventArgs eventArgs)
        {
            if (timer <= 0 && eventArgs is DamageGiveEvent)
            {
                timer = 0.5f;
                foreach (var effect in effects) effect.trigger(target, 1f);
            }
        }

        public override void registerTarget(Entity target, object args = null)
        {
            this.target = target;
            target.registerListener(this);
        }

        public override void removeSelf()
        {
        }

        public override void update(float deltaTime, Entity target)
        {
            if (this.target != target)
            {
                timer -= deltaTime;
                if (timer < 0) timer = 0;
            }
        }
    }
}