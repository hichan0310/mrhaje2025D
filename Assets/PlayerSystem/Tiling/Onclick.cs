// OnClickOnly.cs

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PlayerSystem.Tiling
{

    [AddComponentMenu("Events/OnClick With Two Int Args")]
    public class OnClickWithArgs2 : MonoBehaviour, IPointerClickHandler
    {
        [System.Serializable] public class IntIntEvent : UnityEvent<int, int> {}

        [SerializeField] private int arg1;
        [SerializeField] private int arg2;
        [SerializeField] private IntIntEvent onClick = new IntIntEvent();

        public void OnPointerClick(PointerEventData eventData) => onClick.Invoke(arg1, arg2);

        // 버튼 등에서 직접 호출하고 싶을 때
        public void Invoke() => onClick.Invoke(arg1, arg2);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(OnClickWithArgs2))]
    public class OnClickWithArgs2Editor : Editor
    {
        SerializedProperty arg1Prop, arg2Prop, onClickProp;

        void OnEnable(){
            arg1Prop = serializedObject.FindProperty("arg1");
            arg2Prop = serializedObject.FindProperty("arg2");
            onClickProp = serializedObject.FindProperty("onClick");
        }

        public override void OnInspectorGUI(){
            serializedObject.Update();
            arg1Prop.intValue = EditorGUILayout.IntField("Arg 1", arg1Prop.intValue);
            arg2Prop.intValue = EditorGUILayout.IntField("Arg 2", arg2Prop.intValue);
            EditorGUILayout.PropertyField(onClickProp, new GUIContent("On Click (int, int)"));
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif


}