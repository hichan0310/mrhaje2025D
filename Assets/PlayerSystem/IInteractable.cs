using UnityEngine;

namespace PlayerSystem
{
    public interface IInteractable
    {
        Vector3 WorldPosition { get; }

        // 근접 시
        void OnFocus();

        // 이탈 시
        void OnDefocus();
    }
}

