using UnityEngine;

namespace EntitySystem.HpBar
{
    public class HpBar:MonoBehaviour
    {
        private GameObject progressBar;
        private GameObject progressBound;
        public float length = 1.28f;
        public float ratio { get; set; } = 0;
        private Vector3 startPosition;
        public Transform target { get; set; }

        private void Start()
        {
            progressBar = transform.Find("bar").gameObject;
            progressBound = transform.Find("bound").gameObject;
            startPosition = progressBar.transform.localPosition;
            progressBar.transform.localScale = new Vector3(length / 1.28f, length / 1.28f, length / 1.28f);
            progressBound.transform.localScale = new Vector3(length / 1.28f, length / 1.28f, length / 1.28f);
        }

        private void Update()
        {
            if (!target)
            {
                Destroy(this.gameObject);
            }
            else
            {
                this.transform.position = target.position + Vector3.up;
                ratio = Mathf.Clamp(ratio, 0, 1.0f);
                var scale = progressBar.transform.localScale;
                scale.x = ratio * length / 1.28f;
                progressBar.transform.localScale = scale;
                progressBar.transform.localPosition =
                    new Vector3(length * ratio / 2 - length / 2, 0, 2) + startPosition;
            }
        }
    }
}