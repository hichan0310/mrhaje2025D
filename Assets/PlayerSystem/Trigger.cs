using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    /// <summary>
    /// 트리거 베이스:
    /// - Board를 보유
    /// - 보드의 배치에서 ITriggerEffect를 꺼내 실행
    /// - 이벤트 수신/타겟 등록/업데이트 루프 책임
    /// </summary>
    public abstract class Trigger : IEntityEventListener
    {
        protected Board board;
        protected Entity owner;

        /// <summary>현재 보드 노출(뷰가 읽는다)</summary>
        public Board Board => board;

        /// <summary>보드 위의 효과 나열(캐스팅 성공한 것만)</summary>
        protected IEnumerable<ITriggerEffect> EnumerateEffects()
        {
            if (board == null) yield break;
            var placements = board.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                var poly = board.getPolyomino(placements[i]);
                if (poly is ITriggerEffect eff) yield return eff;
            }
        }

        public abstract void eventActive(EventArgs eventArgs);

        public virtual void registerTarget(Entity target, object args = null)
        {
            owner = target;
            owner.registerListener(this);
        }

        public virtual void removeSelf()
        {
            if (owner != null) owner.removeListener(this);
        }

        public virtual void update(float deltaTime, Entity target) { }
    }
}
