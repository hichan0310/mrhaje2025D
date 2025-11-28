// Assets/EnemySystem/Drone/DroneBase.cs
using UnityEngine;
using EntitySystem;

namespace EnemySystem
{
    /// <summary>
    /// Base class for flying enemies (drones).
    /// Inherits EnemyBase and sets up common flight settings.
    /// </summary>
    public abstract class DroneBase : EnemyBase
    {
        [Header("Drone Movement")]
        [SerializeField] protected float hoverHeight = 0f;
        [SerializeField] protected float verticalDamping = 0.2f;
        [SerializeField] protected float flightSpeed = 2f;
        [SerializeField] protected float chaseSpeed = 3.5f;
        [SerializeField] protected bool useGravity = false;

        protected override void Start()
        {
            base.Start();

            // Disable gravity by default for flying enemies
            if (!useGravity && rb != null)
            {
                rb.gravityScale = 0f;
            }
        }
    }
}
