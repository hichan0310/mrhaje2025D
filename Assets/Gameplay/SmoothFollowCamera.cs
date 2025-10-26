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

        [Header("Vertical Tracking")]
        [Tooltip("If enabled the camera only slowly follows vertical movement while the target remains within the screen.")]
        [SerializeField] private bool limitVerticalTracking = true;

        [Tooltip("How far the target can move vertically (in world units) before the camera starts catching up quickly.")]
        [SerializeField] [Min(0f)] private float verticalDeadZone = 1.5f;

        [Tooltip("Smooth time for the vertical catch-up when the target leaves the dead zone.")]
        [SerializeField] [Min(0.01f)] private float verticalCatchUpSmoothTime = 0.35f;

        [Tooltip("Maximum speed when catching up vertically. Set to 0 to remove the limit.")]
        [SerializeField] [Min(0f)] private float verticalCatchUpMaxSpeed = 12f;

        [Tooltip("Maximum speed while the target is inside the dead zone. Set to 0 to keep the current height completely fixed.")]
        [SerializeField] [Min(0f)] private float verticalIdleSpeed = 0.5f;

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
        private float verticalVelocity;

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
            Vector3 currentPosition = transform.position;

            float maxSpeed = maxPositionSpeed <= 0f ? Mathf.Infinity : maxPositionSpeed;

            currentPosition.x = Mathf.SmoothDamp(currentPosition.x, desiredPosition.x, ref positionVelocity.x, positionSmoothTime, maxSpeed, deltaTime);
            currentPosition.z = Mathf.SmoothDamp(currentPosition.z, desiredPosition.z, ref positionVelocity.z, positionSmoothTime, maxSpeed, deltaTime);

            if (limitVerticalTracking)
            {
                positionVelocity.y = 0f;
                float deltaY = desiredPosition.y - currentPosition.y;
                float absDeltaY = Mathf.Abs(deltaY);
                float catchUpMaxSpeed = verticalCatchUpMaxSpeed <= 0f ? Mathf.Infinity : verticalCatchUpMaxSpeed;

                if (absDeltaY > verticalDeadZone)
                {
                    currentPosition.y = Mathf.SmoothDamp(currentPosition.y, desiredPosition.y, ref verticalVelocity, verticalCatchUpSmoothTime, catchUpMaxSpeed, deltaTime);
                }
                else if (verticalIdleSpeed > 0f)
                {
                    float step = verticalIdleSpeed * deltaTime;
                    currentPosition.y = Mathf.MoveTowards(currentPosition.y, desiredPosition.y, step);
                    verticalVelocity = 0f;
                }
                else
                {
                    verticalVelocity = 0f;
                }
            }
            else
            {
                currentPosition.y = Mathf.SmoothDamp(currentPosition.y, desiredPosition.y, ref positionVelocity.y, positionSmoothTime, maxSpeed, deltaTime);
                verticalVelocity = 0f;
            }

            transform.position = currentPosition;
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
            verticalVelocity = 0f;
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

                if (limitVerticalTracking)
                {
                    desiredLookAhead.y = 0f;
                }

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
            verticalDeadZone = Mathf.Max(0f, verticalDeadZone);
            verticalCatchUpSmoothTime = Mathf.Max(0.01f, verticalCatchUpSmoothTime);
            verticalCatchUpMaxSpeed = Mathf.Max(0f, verticalCatchUpMaxSpeed);
            verticalIdleSpeed = Mathf.Max(0f, verticalIdleSpeed);
        }
#endif
    }
}
