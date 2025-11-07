using UnityEngine;

namespace EntitySystem.Events
{
    public class BasicAttackExecuteEvent : EventArgs, IEntityInfo
    {
        public Entity entity { get; }                 // ← 인터페이스 구현 (필드 X, 프로퍼티 O)
        public Vector3 targetPosition { get; }

        public BasicAttackExecuteEvent(Entity entity, Vector3 targetPosition)
        {
            name = $"BasicAttackExecuteEvent: {entity.name}";
            this.entity = entity;
            this.targetPosition = targetPosition;
        }

        public override void trigger()
        {
            entity.eventActive(this);
        }
    }
}