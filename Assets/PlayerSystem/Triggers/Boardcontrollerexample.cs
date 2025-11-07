using System;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Triggers
{

    // 타일링 보드를 이벤트로 구동하는 얇은 컨트롤러
    public sealed class EventDrivenBoardController : MonoBehaviour, IEntityEventListener
    {
        [Header("Board (배치 전용)")]
        public Board board;
        public int width = 8;
        public int height = 6;

        [Header("Target")]
        public Entity target;

        [Header("Cooldown")]
        public float internalCooldownSec = 0.5f;

        [Header("Power Policy")]
        public bool useConstantPower = false;
        public float constantPower = 1f;

        void Awake()
        {
            if (board == null)
                board = new Board(width, height);
        }

        void Update()
        {
            board.Tick(Time.deltaTime);
        }

        // IEntityEventListener
        public void registerTarget(Entity tgt, object args = null)
        {
            target = tgt;
            target.registerListener(this);
        }

        public void removeSelf()
        {
            target?.removeListener(this);
        }

        public void update(float deltaTime, Entity tgt)
        {
            // 필요 없음(Update에서 Tick)
        }

        public void eventActive(EventArgs eventArgs)
        {
            if (target == null || eventArgs == null) return;

            Func<EventArgs, float?> selector =
                useConstantPower ? PowerPolicies.Const(constantPower) : PowerPolicies.Select;

            board.OnEvent(eventArgs, target, selector, internalCooldownSec);
        }

        // === 배치 헬퍼 ===
        public bool TryPlaceEffect<T>(T effect, int stateIndex, int ax, int ay, out Board.Placement p)
            where T : Polyomino, ITriggerEffect
            => board.TryPlace(effect, stateIndex, ax, ay, out p);

        public bool TryRemoveEffectAt<T>(int ax, int ay, out T effect) where T : Polyomino, ITriggerEffect
        {
            if (board.TryGetPlacementAt(ax, ay, out var placement))
            {
                var poly = board.getPolyomino(in placement);
                if (poly is T t)
                {
                    board.Remove(placement);
                    effect = t;
                    return true;
                }
            }
            effect = null;
            return false;
        }
    }
}
