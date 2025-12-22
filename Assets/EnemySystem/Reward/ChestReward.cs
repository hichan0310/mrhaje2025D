using UnityEngine;
using EntitySystem;

namespace RewardSystem
{
    [CreateAssetMenu(menuName = "Reward/Chest Reward")]
    public class ChestReward : ScriptableObject, IDropReward
    {
        public Chest chestPrefab;

        public void Spawn(Vector3 position, Entity owner = null)
        {
            if (chestPrefab == null) return;

            Instantiate(
                chestPrefab,
                position,
                Quaternion.identity
            );
        }
    }
}
