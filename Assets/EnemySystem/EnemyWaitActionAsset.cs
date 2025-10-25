using UnityEngine;

namespace EnemySystem
{
    [CreateAssetMenu(menuName = "Enemy/Actions/Wait", fileName = "EnemyWaitAction")]
    public class EnemyWaitActionAsset : EnemyActionAsset
    {
        public override void Tick(EnemyController controller, float deltaTime)
        {
            if (!controller)
            {
                return;
            }

            controller.StopMovement();
        }
    }
}
