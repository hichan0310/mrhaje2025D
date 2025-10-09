using UnityEngine;

namespace PlayerSystem
{
    public interface IInteractable
    {
        void Interact(Player player);
        Vector3 WorldPosition { get; }
    }
}
