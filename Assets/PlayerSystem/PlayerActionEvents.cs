using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem
{
    public enum PlayerActionType
    {
        Move,
        Jump,
        DropThrough,
        Fire,
        Skill,
        Ultimate,
        Interact,
        Dodge
    }

    public readonly struct PlayerActionContext
    {
        public PlayerActionContext(
            Player player,
            PlayerActionType actionType,
            Vector2 moveInput,
            Vector2 aimDirection,
            float chargeTime,
            float powerMultiplier,
            bool isJustDodge,
            bool isGrounded,
            Entity target,
            float duration,
            float speed,
            float resolvedPower)
        {
            Player = player;
            ActionType = actionType;
            MoveInput = moveInput;
            AimDirection = aimDirection;
            ChargeTime = chargeTime;
            PowerMultiplier = powerMultiplier;
            IsJustDodge = isJustDodge;
            IsGrounded = isGrounded;
            Target = target;
            Duration = duration;
            Speed = speed;
            ResolvedPower = resolvedPower;
        }

        public Player Player { get; }
        public PlayerActionType ActionType { get; }
        public Vector2 MoveInput { get; }
        public Vector2 AimDirection { get; }
        public float ChargeTime { get; }
        public float PowerMultiplier { get; }
        public bool IsJustDodge { get; }
        public bool IsGrounded { get; }
        public Entity Target { get; }
        public float Duration { get; }
        public float Speed { get; }
        public float ResolvedPower { get; }
    }

    public class PlayerActionEventArgs : EventArgs
    {
        public PlayerActionEventArgs(Player player, PlayerActionContext context)
        {
            name = "PlayerActionEvent";
            Player = player;
            Context = context;
        }

        public Player Player { get; }
        public PlayerActionContext Context { get; }
        public PlayerActionType ActionType => Context.ActionType;
        public float Power => Context.ResolvedPower;

        public override void trigger()
        {
            Player?.eventActive(this);
        }
    }
}
