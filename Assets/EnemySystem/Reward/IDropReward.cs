using UnityEngine;
using EntitySystem;

namespace RewardSystem
{
    public interface IDropReward
    {
        void Spawn(Vector3 position, Entity owner = null);
    }
}
