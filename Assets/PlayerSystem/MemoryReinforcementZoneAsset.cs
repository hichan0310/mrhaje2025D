using System;
using System.Collections.Generic;
using UnityEngine;
using PlayerSystem.Tiling;

namespace PlayerSystem
{
    [CreateAssetMenu(menuName = "Player/Memory/Reinforcement Zone", fileName = "MemoryReinforcementZone")]
    public class MemoryReinforcementZoneAsset : ScriptableObject
    {
        [SerializeField] private string displayName = "Reinforcement";
        [SerializeField] private float bonusPercent = 25f;
        [SerializeField] private List<Vector2Int> shapeCells = new() { Vector2Int.zero };
        [NonSerialized] private Cell[] cachedTilingCells = null;

        public string DisplayName => displayName;
        public float BonusPercent => bonusPercent;
        public IReadOnlyList<Vector2Int> ShapeCells => shapeCells;

        internal IReadOnlyList<Cell> GetTilingCells()
        {
            if (cachedTilingCells == null || cachedTilingCells.Length != shapeCells.Count)
            {
                cachedTilingCells = MemoryPieceTilingUtility.CreateShapeSnapshot(shapeCells);
            }

            return cachedTilingCells;
        }

        private void OnEnable()
        {
            cachedTilingCells = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            cachedTilingCells = null;
        }
#endif
    }
}
