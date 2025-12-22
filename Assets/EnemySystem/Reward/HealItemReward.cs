using UnityEngine;
using EntitySystem;

namespace RewardSystem
{
    [CreateAssetMenu(menuName = "Reward/Heal Item Reward")]
    public class HealItemReward : ScriptableObject, IDropReward
    {
        public GameObject healItemPrefab;

        public void Spawn(Vector3 position, Entity owner = null)
        {
            if (healItemPrefab == null) return;

            Instantiate(
                healItemPrefab,
                position,
                Quaternion.identity
            );
        }
    }
}
