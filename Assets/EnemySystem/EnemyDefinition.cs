using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnemySystem
{
    [CreateAssetMenu(menuName = "Enemy/Definition", fileName = "EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Serializable]
        public struct ActionSequenceEntry
        {
            public EnemyActionAsset action;
            [Tooltip("Duration in seconds. Set to 0 to keep this action active until it requests a transition.")]
            public float duration;
        }

        [Header("Stats")]
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private int baseHealth = 50;
        [SerializeField] private int baseAttack = 10;
        [SerializeField] private int baseDefense = 0;
        [SerializeField] private float moveSpeed = 3f;

        [Header("Behaviour")]
        [SerializeField] private ActionSequenceEntry[] actionSequence = Array.Empty<ActionSequenceEntry>();

        public string DisplayName => displayName;
        public int BaseHealth => Mathf.Max(1, baseHealth);
        public int BaseAttack => Mathf.Max(0, baseAttack);
        public int BaseDefense => Mathf.Max(0, baseDefense);
        public float MoveSpeed => Mathf.Max(0f, moveSpeed);
        public IReadOnlyList<ActionSequenceEntry> ActionSequence => actionSequence;
    }
}
