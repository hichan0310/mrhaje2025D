using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem
{
    [CreateAssetMenu(menuName = "Player/Memory/Reinforcement Zone", fileName = "MemoryReinforcementZone")]
    public class MemoryReinforcementZoneAsset : ScriptableObject
    {
        [SerializeField] private string displayName = "Reinforcement";
        [SerializeField] private float bonusPercent = 25f;
        [SerializeField] private List<Vector2Int> shapeCells = new() { Vector2Int.zero };

        public string DisplayName => displayName;
        public float BonusPercent => bonusPercent;
        public IReadOnlyList<Vector2Int> ShapeCells => shapeCells;
    }
}
