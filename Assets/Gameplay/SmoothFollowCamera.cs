using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Smoothly follows a target transform with optional look-ahead based on the target's motion.
    /// </summary>
    [DisallowMultipleComponent]
    public class SmoothFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform that the camera should follow. If left empty the component will try to find an object tagged 'Player'.")]
        [SerializeField] private Transform target;

        [Header("Position")]
        [Tooltip("Offset from the target position in world space.")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 2f, -10f);

        [Tooltip("Time it takes to reach the target position. Smaller values snap faster, larger values feel smoother.")]
        [SerializeField] [Min(0.01f)] private float positionSmoothTime = 0.2f;

        [Tooltip("Maximum speed of the camera when moving towards the target position.")]
        [SerializeField] [Min(0f)] private float maxPositionSpeed = 40f;

        [Header("Look Ahead")]
        [Tooltip("If enabled, the camera adds a small offset in the direction the target is moving.")]
        [SerializeField] private bool enableLookAhead = true;

        [Tooltip("Maximum distance for the dynamic look-ahead offset.")]
        [SerializeField] [Min(0f)] private float lookAheadDistance = 2.5f;

        [Tooltip("Responsiveness of the look-ahead offset. Higher values react quicker to changes in direction.")]
        [SerializeField] [Min(0.1f)] private float lookAheadResponsiveness = 3f;

        [Tooltip("Smoothing applied to changes in the look-ahead offset.")]
        [SerializeField] [Min(0.01f)] private float lookAheadSmoothTime = 0.15f;

        private Vector3 positionVelocity;
        private Vector3 currentLookAhead;
        private Vector3 lookAheadVelocity;
        private Vector3 lastTargetPosition;
        private bool hasLastTargetPosition;

        private void Awake()
        {
            EnsureTarget();
            if (target != null)
            {
                lastTargetPosition = target.position;
                hasLastTargetPosition = true;
            }
        }

        private void LateUpdate()
        {
            EnsureTarget();
            if (target == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Vector3 targetPosition = target.position;

            if (enableLookAhead)
            {
                UpdateLookAhead(targetPosition, deltaTime);
            }
            else
            {
                currentLookAhead = Vector3.zero;
                lookAheadVelocity = Vector3.zero;
                lastTargetPosition = targetPosition;
                hasLastTargetPosition = true;
            }

            Vector3 desiredPosition = targetPosition + positionOffset + currentLookAhead;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime, maxPositionSpeed <= 0f ? Mathf.Infinity : maxPositionSpeed, deltaTime);
        }

        /// <summary>
        /// Allows changing the follow target at runtime.
        /// </summary>
        /// <param name="newTarget">Transform to follow.</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            positionVelocity = Vector3.zero;
            currentLookAhead = Vector3.zero;
            lookAheadVelocity = Vector3.zero;
            if (target != null)
            {
                lastTargetPosition = target.position;
                hasLastTargetPosition = true;
            }
            else
            {
                hasLastTargetPosition = false;
            }
        }

        private void UpdateLookAhead(Vector3 targetPosition, float deltaTime)
        {
            if (!hasLastTargetPosition)
            {
                lastTargetPosition = targetPosition;
                hasLastTargetPosition = true;
                currentLookAhead = Vector3.zero;
                return;
            }

            Vector3 desiredLookAhead = Vector3.zero;

            if (deltaTime > 0f)
            {
                Vector3 displacement = targetPosition - lastTargetPosition;
                Vector3 velocity = displacement / deltaTime;
                Vector3 planarVelocity = new Vector3(velocity.x, velocity.y, 0f);
                float speed = planarVelocity.magnitude;

                desiredLookAhead = speed > 0.01f
                    ? planarVelocity.normalized * Mathf.Min(lookAheadDistance, speed * lookAheadResponsiveness)
                    : Vector3.zero;

                currentLookAhead = Vector3.SmoothDamp(currentLookAhead, desiredLookAhead, ref lookAheadVelocity, lookAheadSmoothTime, Mathf.Infinity, deltaTime);
            }
            else
            {
                currentLookAhead = desiredLookAhead;
            }

            lastTargetPosition = targetPosition;
        }

        private void EnsureTarget()
        {
            if (target != null)
            {
                return;
            }

            GameObject found = GameObject.FindWithTag("Player");
            if (found != null)
            {
                SetTarget(found.transform);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            positionSmoothTime = Mathf.Max(0.01f, positionSmoothTime);
            lookAheadResponsiveness = Mathf.Max(0.1f, lookAheadResponsiveness);
            lookAheadSmoothTime = Mathf.Max(0.01f, lookAheadSmoothTime);
            maxPositionSpeed = Mathf.Max(0f, maxPositionSpeed);
            lookAheadDistance = Mathf.Max(0f, lookAheadDistance);
        }
#endif
    }
}
