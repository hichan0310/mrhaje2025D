using UnityEngine;

namespace Samples
{
    [ExecuteAlways]
    public class SimpleFillBar : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
        [SerializeField] private Transform fillTransform = null;
        [SerializeField] private bool anchorLeft = true;

        private Vector3 initialScale = Vector3.one;
        private Vector3 initialLocalPosition = Vector3.zero;

        private void Awake()
        {
            CacheInitialState();
            ApplyFill();
        }

        private void OnEnable()
        {
            CacheInitialState();
            ApplyFill();
        }

        private void OnValidate()
        {
            CacheInitialState();
            ApplyFill();
        }

        private void CacheInitialState()
        {
            if (!fillTransform)
            {
                return;
            }

            initialScale = fillTransform.localScale;
            initialLocalPosition = fillTransform.localPosition;
        }

        public void SetFill(float value)
        {
            fillAmount = Mathf.Clamp01(value);
            ApplyFill();
        }

        private void ApplyFill()
        {
            if (!fillTransform)
            {
                return;
            }

            float clamped = Mathf.Clamp01(fillAmount);
            var scale = initialScale;
            scale.x = initialScale.x * clamped;
            fillTransform.localScale = scale;

            if (anchorLeft)
            {
                float offset = (initialScale.x - scale.x) * 0.5f;
                var position = initialLocalPosition;
                position.x = initialLocalPosition.x - offset;
                fillTransform.localPosition = position;
            }
        }
    }
}
