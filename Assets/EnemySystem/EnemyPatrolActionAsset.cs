using UnityEngine;

namespace EnemySystem
{
    [CreateAssetMenu(menuName = "Enemy/Actions/Patrol", fileName = "EnemyPatrolAction")]
    public class EnemyPatrolActionAsset : EnemyActionAsset
    {
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float flipInterval = 2f;

        private float timer = 0f;

        public override void OnEnter(EnemyController controller)
        {
            timer = 0f;
        }

        public override void Tick(EnemyController controller, float deltaTime)
        {
            if (!controller)
            {
                return;
            }

            Vector2 velocity = new Vector2(controller.PatrolDirection * controller.MoveSpeed * speedMultiplier, 0f);
            controller.Move(velocity, deltaTime);

            if (flipInterval <= 0f)
            {
                return;
            }

            timer += deltaTime;
            if (timer >= flipInterval)
            {
                timer = 0f;
                controller.InvertPatrolDirection();
            }
        }
    }
}
