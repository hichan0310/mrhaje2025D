// ChestDropEntry.cs
using UnityEngine;

namespace RewardSystem
{
    [System.Serializable]
    public class ChestDropEntry
    {
        [Range(0f, 1f)]
        public float probability;

        [Tooltip("reward Polyomino가 이미 세팅된 Chest 프리팹")]
        public Chest chestPrefab;
    }
}
