using PlayerSystem.Weapons;
using UnityEngine;

namespace EnemySystem
{
    [CreateAssetMenu(menuName = "Enemy/Actions/Shoot Projectile", fileName = "EnemyShootProjectileAction")]
    public class EnemyShootProjectileActionAsset : EnemyActionAsset
    {
        [SerializeField] private Projectile projectilePrefab = null;
        [SerializeField] private float projectilePower = 1f;
        [SerializeField] private float cooldown = 1.5f;

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

            timer -= deltaTime;
            if (timer > 0f)
            {
                return;
            }

            controller.FireProjectile(projectilePrefab, projectilePower);
            timer = Mathf.Max(0.1f, cooldown);
        }
    }
}
