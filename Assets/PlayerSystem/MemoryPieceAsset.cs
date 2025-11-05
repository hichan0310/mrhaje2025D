using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using PlayerSystem.Effects;

namespace PlayerSystem
{
    /// <summary>
    /// A single memory piece definition.
    /// - 어떤 이벤트나 상황에서도 발동 가능 (트리거 제약 없음)
    /// - 단순히 효과, 파워, 쿨타임, 리소스, 도형 정보만 가짐
    /// </summary>
    [CreateAssetMenu(menuName = "Player/Memory/Piece", fileName = "MemoryPiece")]
    public class MemoryPieceAsset : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private string displayName = "Memory Piece";
        [SerializeField] private Sprite icon = null;
        [TextArea][SerializeField] private string description = string.Empty;

        [Header("Effect Settings")]
        [SerializeField] private TriggerEffectAsset effect = null;
        [SerializeField] private float basePower = 1f;
        [SerializeField] private float cooldownSeconds = 0f;

        [Header("Resource Settings")]
        [SerializeField] private MemoryResourceType resourceType = MemoryResourceType.None;
        [SerializeField] private float resourceCost = 0f;
        [SerializeField] private bool isCore = false;

        [Header("Shape Settings")]
        [SerializeField] private List<Vector2Int> shapeCells = new() { Vector2Int.zero };

        // === Properties ===
        public string DisplayName => displayName;
        public TriggerEffectAsset Effect => effect;
        public float BasePower => basePower;
        public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
        public MemoryResourceType ResourceType => resourceType;
        public float ResourceCost => Mathf.Max(0f, resourceCost);
        public bool IsCore => isCore;
        public Sprite Icon => icon;
        public string Description => description;
        public IReadOnlyList<Vector2Int> ShapeCells => shapeCells;

#if UNITY_EDITOR
        private void OnValidate()
        {
            basePower = Mathf.Max(0f, basePower);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            resourceCost = Mathf.Max(0f, resourceCost);

            if (shapeCells == null || shapeCells.Count == 0)
                shapeCells = new List<Vector2Int> { Vector2Int.zero };
        }
#endif
    }
}
