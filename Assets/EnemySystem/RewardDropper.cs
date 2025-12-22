// ChestDropper.cs
using UnityEngine;
using EntitySystem;

namespace RewardSystem
{
    public static class ChestDropper
    {
        public static void TryDrop(EnemyBase enemy)
        {
            if (enemy == null) return;

            var holder = enemy.GetComponent<ChestDropTableHolder>();
            if (holder == null || holder.dropTable == null) return;

            ChestDropEntry entry = holder.dropTable.Roll();
            if (entry == null || entry.chestPrefab == null) return;

            Object.Instantiate(
                entry.chestPrefab,
                enemy.transform.position,
                Quaternion.identity
            );
        }
    }
}
