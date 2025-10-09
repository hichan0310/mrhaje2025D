using System.Collections.Generic;
using UnityEngine;
using PlayerSystem.Effects;

namespace PlayerSystem
{
    [CreateAssetMenu(menuName = "Player/Memory/Piece", fileName = "MemoryPiece")]
    public class MemoryPieceAsset : ScriptableObject
    {
        [SerializeField] private string displayName = "Memory Piece";
        [SerializeField] private ActionTriggerType triggerType = ActionTriggerType.None;
        [SerializeField] private TriggerEffectAsset effect = null;
        [SerializeField] private float basePower = 1f;
        [SerializeField] private float cooldownSeconds = 0f;
        [SerializeField] private MemoryResourceType resourceType = MemoryResourceType.None;
        [SerializeField] private float resourceCost = 0f;
        [SerializeField] private bool isCore = false;
        [SerializeField] private Sprite icon = null;
        [TextArea]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private List<Vector2Int> shapeCells = new() { Vector2Int.zero };

        public string DisplayName => displayName;
        public ActionTriggerType TriggerType => triggerType;
        public TriggerEffectAsset Effect => effect;
        public float BasePower => basePower;
        public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
        public MemoryResourceType ResourceType => resourceType;
        public float ResourceCost => Mathf.Max(0f, resourceCost);
        public bool IsCore => isCore;
        public Sprite Icon => icon;
        public string Description => description;
        public IReadOnlyList<Vector2Int> ShapeCells => shapeCells;
    }
}
