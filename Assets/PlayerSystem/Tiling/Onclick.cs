// OnClickOnly.cs

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PlayerSystem.Tiling
{
    public class OnClickWithArgs2 : MonoBehaviour, IPointerClickHandler
    {
        [System.Serializable] public class IntIntEvent : UnityEvent<int, int> {}

        [SerializeField] public int arg1;
        [SerializeField] public int arg2;
        [SerializeField] private IntIntEvent onClick = new IntIntEvent();

        public void OnPointerClick(PointerEventData eventData) => onClick.Invoke(arg1, arg2);

        // 버튼 등에서 직접 호출하고 싶을 때
        // public void Invoke() => onClick.Invoke(arg1, arg2);
    }
}