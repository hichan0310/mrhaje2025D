// Assets/PlayerSystem/Triggers/SimpleTriggerExample.cs (발췌/갱신)
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    public class SimpleTriggerExample : Trigger, IBoardEditableTrigger
    {
        private float timer;
        public Entity target;

        private void Awake()
        {
            this.board = new Board(8, 6);
        }

        // === IBoardEditableTrigger 구현 ===
        public bool TryAdd(object effectPolyomino, int stateIndex, int x, int y)
        {
            return board.TryPlace(effectPolyomino, stateIndex, x, y, out _);
        }

        public bool TryRemove(int x, int y)
        {
            return board.TryRemoveAt(x, y, out _);
        }

        // === 이벤트 처리 ===
        public override void eventActive(EventArgs eventArgs)
        {
            if (owner == null || !ReferenceEquals(owner, target)) return;

            if (timer <= 0 && eventArgs is DamageGiveEvent)
            {
                timer = 0.5f;
                foreach (var effect in EnumerateEffects())
                    effect.trigger(target, 1f);
            }
        }

        public override void registerTarget(Entity target, object args = null)
        {
            this.target = target;
            base.registerTarget(target, args);
        }

        public override void update(float deltaTime, Entity target)
        {
            if (this.target == target)
            {
                timer -= deltaTime;
                if (timer < 0) timer = 0;
            }
        }
    }
}
