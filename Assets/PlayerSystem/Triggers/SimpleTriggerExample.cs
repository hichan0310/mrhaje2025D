using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    /// <summary>
    /// 간단 예시:
    /// - DamageGiveEvent를 받으면(내부 쿨 0.5초) 보드 위 모든 ITriggerEffect에 power=1f로 trigger 호출
    /// - 배치/제거는 프론트(UI)에서 이 컴포넌트의 API로 호출
    /// </summary>
    public class SimpleTriggerExample : Trigger
    {
        private float timer;
        public Entity target;

        private void Awake()
        {
            this.board = new Board(8, 6);
        }

        // --- 프론트(UI)에서 쓰는 API ---

        public bool tryAddEffect<T>(T effect, int stateIndex, int ax, int ay, out Board.Placement placement)
            where T : class // Polyomino & ITriggerEffect를 동시에 만족하는 타입을 전달
        {
            return this.board.TryPlace(effect, stateIndex, ax, ay, out placement);
        }

        public bool tryRemoveEffect<T>(int ax, int ay, out T effect)
            where T : class // Polyomino & ITriggerEffect
        {
            if (board.TryRemoveAt(ax, ay, out var removed))
            {
                var poly = board.getPolyomino(in removed);
                effect = poly as T;
                return effect != null;
            }
            effect = null;
            return false;
        }

        // --- 이벤트 수신 ---

        public override void eventActive(EntitySystem.Events.EventArgs eventArgs)
        {
            if (owner == null || !ReferenceEquals(owner, target)) return;

            if (timer <= 0f && eventArgs is DamageGiveEvent)
            {
                timer = 0.5f; // 내부 쿨
                foreach (var effect in EnumerateEffects())
                    effect.trigger(target, 1f);
            }
        }

        // --- 타겟 등록/업데이트 ---

        public override void registerTarget(Entity target, object args = null)
        {
            this.target = target;
            base.registerTarget(target, args);
        }

        public override void update(float deltaTime, Entity target)
        {
            // 타이머는 내 타겟일 때만 감소
            if (this.target == target)
            {
                timer -= deltaTime;
                if (timer < 0f) timer = 0f;
            }
        }
    }
}
