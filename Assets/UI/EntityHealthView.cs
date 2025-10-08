using EntitySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frontend
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class EntityHealthView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField]
        private Vector2 defaultSize = new Vector2(320f, 72f);

        [SerializeField]
        private float labelHeight = 28f;

        [SerializeField]
        private float barHeight = 36f;

        [Header("Colors")]
        [SerializeField]
        private Color backgroundColor = new Color(0f, 0f, 0f, 0.45f);

        [SerializeField]
        private Color fullHealthColor = new Color(0.2f, 0.73f, 0.43f, 1f);

        [SerializeField]
        private Color lowHealthColor = new Color(0.85f, 0.23f, 0.23f, 1f);

        [Header("Typography")]
        [SerializeField]
        private float labelFontSize = 22f;

        [SerializeField]
        private float valueFontSize = 20f;

        private TextMeshProUGUI nameLabel;
        private TextMeshProUGUI valueLabel;
        private Image fillImage;
        private Entity entity;
        private string displayName = "Entity";

        private void Awake()
        {
            BuildView();
            Refresh();
        }

        public void Bind(Entity target, string nameOverride = null)
        {
            entity = target;
            if (!string.IsNullOrEmpty(nameOverride))
            {
                displayName = nameOverride;
            }
            else if (entity != null)
            {
                displayName = string.IsNullOrWhiteSpace(entity.name) ? displayName : entity.name;
            }

            Refresh();
        }

        public void SetDisplayName(string nameOverride)
        {
            if (!string.IsNullOrEmpty(nameOverride))
            {
                displayName = nameOverride;
            }
        }

        private void Update()
        {
            Refresh();
        }

        private void BuildView()
        {
            var rect = (RectTransform)transform;
            if (rect.sizeDelta == Vector2.zero)
            {
                rect.sizeDelta = defaultSize;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);

            nameLabel = CreateText("NameLabel", rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            nameLabel.fontSize = labelFontSize;
            nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
            nameLabel.margin = new Vector4(12f, 0f, 12f, 0f);

            var nameRect = nameLabel.rectTransform;
            nameRect.offsetMin = new Vector2(0f, -labelHeight);
            nameRect.offsetMax = Vector2.zero;

            var barRect = CreateRectTransform("BarArea", rect);
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.offsetMin = new Vector2(0f, 0f);
            barRect.offsetMax = new Vector2(0f, barHeight);

            var background = CreateImage("BarBackground", barRect);
            background.color = backgroundColor;
            background.raycastTarget = false;

            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0f);
            backgroundRect.anchorMax = new Vector2(1f, 1f);
            backgroundRect.offsetMin = new Vector2(8f, 6f);
            backgroundRect.offsetMax = new Vector2(-8f, -6f);

            var fillRect = CreateRectTransform("Fill", backgroundRect);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.color = fullHealthColor;
            fillImage.raycastTarget = false;

            valueLabel = CreateText("ValueLabel", backgroundRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            valueLabel.fontSize = valueFontSize;
            valueLabel.alignment = TextAlignmentOptions.Center;
            valueLabel.margin = new Vector4(0f, 0f, 0f, 0f);
            valueLabel.raycastTarget = false;
            valueLabel.transform.SetAsLastSibling();
        }

        private TextMeshProUGUI CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.enableWordWrapping = false;

            return text;
        }

        private RectTransform CreateRectTransform(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Image CreateImage(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        private void Refresh()
        {
            if (nameLabel != null)
            {
                nameLabel.text = displayName;
            }

            if (valueLabel == null || fillImage == null)
            {
                return;
            }

            if (entity == null || entity.stat == null)
            {
                fillImage.fillAmount = 0f;
                fillImage.color = lowHealthColor;
                valueLabel.text = "- / -";
                return;
            }

            var stat = entity.stat;
            var maxHp = Mathf.Max(0, stat.maxHp);
            var currentHp = Mathf.Clamp(stat.nowHp, 0, maxHp);
            var ratio = maxHp <= 0 ? 0f : (float)currentHp / maxHp;
            ratio = Mathf.Clamp01(ratio);

            fillImage.fillAmount = ratio;
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, ratio);
            valueLabel.text = $"{currentHp} / {maxHp}";
        }
    }
}
