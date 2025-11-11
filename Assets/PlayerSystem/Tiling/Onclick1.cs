// OnClickOnly.cs

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PlayerSystem.Tiling
{

    [AddComponentMenu("Events/OnClick With Two Int Args")]
    public class OnClickWithArgs1 : MonoBehaviour, IPointerClickHandler
    {
        [System.Serializable] public class IntIntEvent : UnityEvent<int> {}

        [SerializeField] private int arg1;
        [SerializeField] private IntIntEvent onClick = new IntIntEvent();

        public void OnPointerClick(PointerEventData eventData) => onClick.Invoke(arg1);

        // 버튼 등에서 직접 호출하고 싶을 때
        public void Invoke() => onClick.Invoke(arg1);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(OnClickWithArgs2))]
    public class OnClickWithArgs1Editor : Editor
    {
        SerializedProperty arg1Prop, arg2Prop, onClickProp;

        void OnEnable(){
            arg1Prop = serializedObject.FindProperty("arg1");
            onClickProp = serializedObject.FindProperty("onClick");
        }

        public override void OnInspectorGUI(){
            serializedObject.Update();
            arg1Prop.intValue = EditorGUILayout.IntField("Arg 1", arg1Prop.intValue);
            EditorGUILayout.PropertyField(onClickProp, new GUIContent("On Click (int)"));
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif


}