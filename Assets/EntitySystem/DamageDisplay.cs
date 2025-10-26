using EntitySystem.Events;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EntitySystem
{
    [RequireComponent(typeof(Transform))]
    public class DamageDisplay : MonoBehaviour
    {
        [Header("Motion/Fade")]
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private float destroyTime = 0.8f;
        [SerializeField] private float fadeStart = 0.4f;

        [Header("Visual")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color critColor = new Color(1f, 0.95f, 0.2f);
        [SerializeField] private float normalFontSize = 4f;
        [SerializeField] private float critFontSize = 6f;

        [Header("Rendering (World Text)")]
        [SerializeField] private bool isUIOverlay = false; 
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 5000;

        private float _timer;
        private TMP_Text _text;
        private Camera _cam;
        private RectTransform _rt;

        public DamageTakeEvent dmgEvent
        {
            set
            {
                if (_text == null) CacheText();

                if (value == null || value.target == null)
                {
                    Debug.LogWarning("[DamageDisplay] dmgEvent or target was null");
                    Destroy(gameObject);
                    return;
                }


                float x = Random.Range(-0.3f, 0.3f);
                float y = Random.Range(-0.3f, 0.3f);
                var worldPos = value.target.transform.position + new Vector3(x, y, 0f);

                if (isUIOverlay)
                {

                    EnsureCamera();
                    if (_rt == null) _rt = transform as RectTransform;

                    if (_cam != null && _rt != null)
                    {
                        Vector3 screen = RectTransformUtility.WorldToScreenPoint(_cam, worldPos);
                        _rt.position = screen;
                    }
                    else
                    {
                        transform.position = worldPos; 
                    }
                }
                else
                {

                    transform.position = worldPos;
                }

                _text.text = value.realDmg.ToString();

                bool isCrit = value.atkTags != null && value.atkTags.Contains(AtkTags.criticalHit);
                _text.fontSize = isCrit ? critFontSize : normalFontSize;
                _text.color = isCrit ? critColor : normalColor;

                _timer = 0f;
            }
        }

        private void Awake()
        {
            CacheText();
            EnsureCamera();


            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = sortingLayerName;
                mr.sortingOrder = sortingOrder;
            }

            // 파괴 예약
            Invoke(nameof(DestroySelf), destroyTime);
        }

        private void EnsureCamera()
        {
            if (_cam != null) return;
            _cam = Camera.main;
        }

        private void CacheText()
        {

            _text = GetComponent<TMP_Text>();
            if (_text == null) _text = GetComponentInChildren<TMP_Text>();

            if (_text == null)
            {
                Debug.LogError("[DamageDisplay] TMP_Text component not found. Destroying self.");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (_text == null) return;

            _timer += Time.deltaTime;


            transform.Translate(0f, moveSpeed * Time.deltaTime, 0f);


            if (_timer >= fadeStart)
            {
                float t = Mathf.InverseLerp(fadeStart, destroyTime, _timer);
                var col = _text.color;
                col.a = 1f - t;
                _text.color = col;
            }
        }

        private void LateUpdate()
        {
            if (isUIOverlay) return;

            EnsureCamera();
            if (_cam == null) return;

            var camTransform = _cam.transform;
            // Billboard toward the camera so the text is readable regardless of view angle.
            transform.rotation = Quaternion.LookRotation(transform.position - camTransform.position, camTransform.up);
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
