using System.Collections.Generic;
using UnityEngine;

namespace RewardSystem
{
    [CreateAssetMenu(
        fileName = "DropTable",
        menuName = "Reward/Drop Table"
    )]
    public class DropTable : ScriptableObject
    {
        public List<DropEntry> entries = new();

        public IDropReward Roll()
        {
            float roll = Random.value;
            float acc = 0f;

            foreach (var e in entries)
            {
                acc += e.probability;
                if (roll <= acc)
                    return e.reward as IDropReward;
            }

            return null;
        }
    }
}

