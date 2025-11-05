// Assets/PlayerSystem/Triggers/Trigger.cs
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Triggers
{
    /// <summary>
    /// - MonoBehaviour로 바꿔서 UnityObject null 비교, isActiveAndEnabled 사용 가능
    /// - IEntityEventListener 구현
    /// - Board는 순수 데이터 모델
    /// </summary>
    public abstract class Trigger : MonoBehaviour, IEntityEventListener
    {
        protected Board board;
        protected Entity owner;

        public Board Board => board;

        /// <summary>보드에 담긴 폴리오미노 중 ITriggerEffect만 열거</summary>
        protected System.Collections.Generic.IEnumerable<ITriggerEffect> EnumerateEffects()
        {
            if (board == null) yield break;
            var placements = board.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                var poly = board.getPolyomino(placements[i]);
                if (poly is ITriggerEffect eff) yield return eff;
            }
        }

        // ---- IEntityEventListener ----
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

        public virtual void update(float deltaTime, Entity target)
        {
            // 필요하면 파생 트리거에서 오버라이드
        }
    }
}
