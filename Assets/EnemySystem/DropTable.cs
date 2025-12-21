// ChestDropTable.cs
using System.Collections.Generic;
using UnityEngine;

namespace RewardSystem
{
    [CreateAssetMenu(
        fileName = "ChestDropTable",
        menuName = "Reward/Chest Drop Table"
    )]
    public class ChestDropTable : ScriptableObject
    {
        public List<ChestDropEntry> entries = new();

        public ChestDropEntry Roll()
        {
            float roll = Random.value;
            float acc = 0f;

            foreach (var e in entries)
            {
                acc += e.probability;
                if (roll <= acc)
                    return e;
            }
            return null;
        }
    }
}
