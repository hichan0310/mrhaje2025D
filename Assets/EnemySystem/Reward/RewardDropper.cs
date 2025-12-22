using UnityEngine;
using EntitySystem;

namespace RewardSystem
{
    public static class Dropper
    {
        public static void TryDrop(EnemyBase enemy)
        {
            if (enemy == null) return;

            var holder = enemy.GetComponent<DropTableHolder>();
            if (holder == null || holder.dropTable == null) return;

            var reward = holder.dropTable.Roll();
            if (reward == null) return;

            reward.Spawn(enemy.transform.position, enemy);
        }
    }
}
