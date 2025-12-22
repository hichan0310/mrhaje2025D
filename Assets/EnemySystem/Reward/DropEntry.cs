using UnityEngine;

namespace RewardSystem
{
    [System.Serializable]
    public class DropEntry
    {
        [Range(0f, 1f)]
        public float probability;

        public ScriptableObject reward; // IDropReward
    }
}
