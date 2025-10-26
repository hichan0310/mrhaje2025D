using UnityEngine;

namespace EnemySystem
{
    public abstract class EnemyActionAsset : ScriptableObject
    {
        public virtual void OnEnter(EnemyController controller)
        {
        }

        public virtual void OnExit(EnemyController controller)
        {
        }

        public abstract void Tick(EnemyController controller, float deltaTime);
    }
}
