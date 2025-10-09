using System;

namespace PlayerSystem
{
    /// <summary>
    /// Defines all player actions that can raise trigger effects.
    /// The design document references these actions when configuring memories.
    /// </summary>
    [Flags]
    public enum ActionTriggerType
    {
        None = 0,
        Jump = 1 << 0,
        DropDown = 1 << 1,
        Shoot = 1 << 2,
        Skill = 1 << 3,
        Interact = 1 << 4,
        Dodge = 1 << 5,
        Dash = 1 << 6,
        Hit = 1 << 7,
    }
}
