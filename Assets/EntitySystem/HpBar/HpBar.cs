using UnityEngine;

namespace EntitySystem.HpBar
{
    public class HpBar : MonoBehaviour
    {
        private GameObject progressBar;
        private GameObject progressBound;

        public float length = 1.28f;
        public float ratio { get; set; } = 0;

        private Vector3 startPosition;
        public Transform target { get; set; }

        [Header("Auto Offset")]
        [SerializeField] private float extraYOffset = 0.3f;

        private SpriteRenderer targetSprite;

        private void Start()
        {
            progressBar = transform.Find("bar").gameObject;
            progressBound = transform.Find("bound").gameObject;

            startPosition = progressBar.transform.localPosition;

            float scale = length / 1.28f;
            progressBar.transform.localScale = Vector3.one * scale;
            progressBound.transform.localScale = Vector3.one * scale;

            if (target)
                targetSprite = target.GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (!target)
            {
                Destroy(gameObject);
                return;
            }

            float yOffset = 1f;
            if (targetSprite != null)
                yOffset = targetSprite.bounds.extents.y + extraYOffset;

            transform.position = target.position + Vector3.up * yOffset;

            ratio = Mathf.Clamp01(ratio);

            var scale = progressBar.transform.localScale;
            scale.x = ratio * length / 1.28f;
            progressBar.transform.localScale = scale;

            progressBar.transform.localPosition =
                new Vector3(length * ratio / 2 - length / 2, 0, 2) + startPosition;
        }
    }
}
