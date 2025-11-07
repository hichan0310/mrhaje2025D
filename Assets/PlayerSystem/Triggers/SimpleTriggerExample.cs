using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Triggers
{
    // DamageGiveEvent 발생 시 power=1로 보드 이펙트 실행 예시
    public class SimpleTriggerExample : Trigger
    {
        public Entity target;

        private void Awake()
        {
            this.board = new Board(8, 6);
        }

        public bool tryAddEffect<T>(T effect, int stateIndex, int ax, int ay, out Board.Placement placement)
            where T : Polyomino, ITriggerEffect
            => this.board.TryPlace(effect, stateIndex, ax, ay, out placement);

        public bool tryRemoveEffect<T>(int ax, int ay, out T effect) where T : Polyomino, ITriggerEffect
        {
            if (board.TryGetPlacementAt(ax, ay, out var placement))
            {
                var poly = board.getPolyomino(in placement);
                if (poly is ITriggerEffect t)
                {
                    effect = (T)t;
                    board.Remove(placement);
                    return true;
                }
            }
            effect = null;
            return false;
        }

        public override void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent)
            {
                board.OnEvent(eventArgs, target, PowerPolicies.One, internalCooldownSec: 0.5f);
            }
        }

        public override void registerTarget(Entity target, object args = null)
        {
            this.target = target;
            target.registerListener(this);
        }

        public override void removeSelf() { }

        public override void update(float deltaTime, Entity target)
        {
            board.Tick(deltaTime);
        }
    }
}
