using PlayerSystem;
using UnityEngine;

namespace Frontend
{
    [DisallowMultipleComponent]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Vector2 offset = new Vector2(0f, 1.5f);

        [SerializeField]
        private float followSpeed = 6f;

        [SerializeField]
        private float maxStepPerSecond = 20f;

        [SerializeField]
        private bool snapOnStart = true;

        private bool hasSnapped;

        private void OnEnable()
        {
            hasSnapped = false;
        }

        private void Reset()
        {
            hasSnapped = false;
            if (target == null)
            {
                var player = FindObjectOfType<Player>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (TryGetComponent(out Camera cameraComponent))
            {
                cameraComponent.orthographic = true;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            if (snapOnStart && !hasSnapped)
            {
                transform.position = desired;
                hasSnapped = true;
                return;
            }

            var current = transform.position;
            var maxStep = maxStepPerSecond * Time.deltaTime;
            var step = followSpeed * Time.deltaTime;
            step = Mathf.Min(step, maxStep);
            transform.position = Vector3.MoveTowards(current, desired, step);
        }
    }
}
