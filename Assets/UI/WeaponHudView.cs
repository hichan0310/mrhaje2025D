using System.Collections.Generic;
using PlayerSystem.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frontend
{
    /// <summary>Compact combat readout. Weapons own their view and supply independent resource entries.</summary>
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup))]
    public class WeaponHudView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite barSprite;
        [SerializeField, Range(600, 760)] private float panelWidth = 600;
        [SerializeField] private Vector2 edgePadding = new Vector2(0, 24);
        [SerializeField] private Color skillColor = new Color(0.42f, 0.85f, 0.95f);
        [SerializeField] private Color ultimateColor = new Color(1, 0.77f, 0.39f);
        private static readonly Color Ink = new Color(0.91f, 0.94f, 0.96f);
        private static readonly Color Muted = new Color(0.68f, 0.74f, 0.78f);
        private readonly WeaponHudState state = new();
        private readonly List<Resource> resources = new();
        private Weapon owner;
        private bool bound;
        private RectTransform safeArea, panel;
        private TextMeshProUGUI title;
        private Image icon;
        private CanvasGroup group;
        private Rect lastSafeArea;
        private Vector2 lastScreen;
        public Weapon Owner => owner;

        private sealed class Resource
        {
            public RectTransform Root, Gauge;
            public TextMeshProUGUI Key, Label, Value, Status;
            public Image Fill;
            public WeaponHudArc Arc;
            public readonly List<Image> Pips = new();
            public readonly List<Image> PipFills = new();
        }

        public void Bind(Weapon weapon) { owner = weapon; bound = true; Refresh(); }
        private void LateUpdate() { if (bound) Refresh(); UpdateSafeArea(); }
        public void Refresh()
        {
            if (!owner) { if (group) group.alpha = 0; return; }
            state.Clear(); owner.WriteHud(state); Render(state);
        }

        public virtual void Render(WeaponHudState data)
        {
            Build();
            group.alpha = data.Enabled ? 1 : 0.4f;
            title.text = data.Title;
            icon.sprite = data.Icon; icon.enabled = data.Icon;
            float extraY = 104;
            for (int i = 0; i < data.Entries.Count; i++)
            {
                if (resources.Count <= i) resources.Add(CreateResource(i));
                var row = resources[i];
                row.Root.gameObject.SetActive(true);
                var entry = data.Entries[i];
                if (i >= 2)
                {
                    Place(row.Root, new Vector2(136, -extraY), new Vector2(panelWidth - 136, 27));
                    extraY += 29;
                }
                Draw(row, entry, i, data.Enabled);
            }
            for (int i = data.Entries.Count; i < resources.Count; i++) resources[i].Root.gameObject.SetActive(false);
            panel.sizeDelta = new Vector2(panelWidth, Mathf.Max(108, extraY));
            UpdateSafeArea();
        }

        private void Build()
        {
            if (panel) return;
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            group = GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            safeArea = Rect("SafeArea", transform);
            safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            panel = Rect("WeaponPanel", safeArea);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0);
            panel.pivot = new Vector2(0.5f, 0);
            panel.anchoredPosition = new Vector2(edgePadding.x, edgePadding.y);
            icon = Picture("WeaponSilhouette", panel, new Vector2(0, -9), new Vector2(112, 42), Color.white);
            icon.preserveAspect = true;
            title = Text("WeaponName", panel, 15, new Vector2(0, -57), new Vector2(124, 24), Ink);
            Picture("WeaponUnderline", panel, new Vector2(0, -84), new Vector2(110, 1), Muted * new Color(1, 1, 1, 0.5f));
        }

        private Resource CreateResource(int index)
        {
            bool extra = index >= 2;
            var row = new Resource { Root = Rect("Resource", panel) };
            float x = index == 0 ? 136 : 350;
            Place(row.Root, new Vector2(x, 0), new Vector2(panelWidth - x, 86));
            row.Key = Text("Key", row.Root, 22, new Vector2(4, -27), new Vector2(30, 32), Ink);
            row.Key.alignment = TextAlignmentOptions.Center;
            row.Label = Text("Label", row.Root, extra ? 12 : 13, new Vector2(extra ? 0 : 48, extra ? 0 : -5), new Vector2(180, 20), Muted);
            row.Value = Text("Value", row.Root, extra ? 14 : 22, new Vector2(extra ? 155 : 48, extra ? 0 : -26), new Vector2(146, 29), Ink);
            row.Status = Text("Status", row.Root, 11, new Vector2(extra ? 238 : 48, extra ? 1 : -72), new Vector2(extra ? 220 : 190, 17), Muted);
            row.Gauge = Rect("Gauge", row.Root); Place(row.Gauge, new Vector2(extra ? 0 : 48, extra ? -24 : -61), new Vector2(extra ? 210 : 150, 4));
            var track = row.Gauge.gameObject.AddComponent<Image>();track.color = new Color(.25f,.32f,.38f,.6f);track.raycastTarget=false;
            row.Fill = Picture("Fill", row.Gauge, Vector2.zero, row.Gauge.sizeDelta, skillColor);
            row.Fill.sprite = barSprite; row.Fill.type = Image.Type.Filled; row.Fill.fillMethod = Image.FillMethod.Horizontal;
            var arcRect = Rect("EnergyRing", row.Root); Place(arcRect, new Vector2(-9, -17), new Vector2(56, 56));
            row.Arc = arcRect.gameObject.AddComponent<WeaponHudArc>();row.Arc.raycastTarget=false;
            return row;
        }

        private void Draw(Resource row, WeaponHudEntry entry, int index, bool enabled)
        {
            Color accent = entry.Accent == WeaponHudAccent.Ultimate ? ultimateColor :
                entry.Accent == WeaponHudAccent.Effect ? new Color(.75f,.7f,1) : skillColor;
            row.Key.gameObject.SetActive(index < 2);
            row.Key.text = entry.Key;
            row.Key.color = entry.Ready && enabled || entry.Active ? accent : Ink;
            row.Label.text = index >= 2 && !string.IsNullOrEmpty(entry.Key) ? $"{entry.Label} [{entry.Key}]" : entry.Label; row.Value.text = entry.Value;
            row.Value.color = entry.Ready && enabled || entry.Active ? accent : Ink;
            row.Status.text = entry.Active ? entry.Status : entry.Ready && entry.Capacity == 0 ? "준비" : entry.Status;
            row.Arc.gameObject.SetActive(index == 1);
            if (index == 1) { row.Arc.color = accent; row.Arc.SetState(entry.Fill, entry.Threshold); }
            row.Gauge.gameObject.SetActive(entry.ShowGauge && index != 1);
            row.Fill.color = accent; row.Fill.fillAmount = entry.Fill;
            row.Fill.gameObject.SetActive(entry.Capacity == 0);
            int count = Mathf.Clamp(entry.Capacity, 0, 24);
            for (int i = 0; i < count; i++)
            {
                if (row.Pips.Count <= i)
                {
                    var pip = Picture("Charge", row.Gauge, Vector2.zero, Vector2.one, new Color(.25f,.32f,.38f,.8f));row.Pips.Add(pip);
                    var fill = Picture("ChargeFill", pip.transform, Vector2.zero, Vector2.one, accent);
                    fill.sprite=barSprite;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;row.PipFills.Add(fill);
                }
                float width = (row.Gauge.sizeDelta.x - (count - 1) * 5) / count;
                row.Pips[i].gameObject.SetActive(true);
                Place(row.Pips[i].rectTransform, new Vector2(i * (width + 5), 0), new Vector2(width, 4));
                Place(row.PipFills[i].rectTransform, Vector2.zero, new Vector2(width, 4));
                row.PipFills[i].color = accent;
                row.PipFills[i].fillAmount = i < entry.Charges ? 1 : i == entry.Charges ? entry.Fill : 0;
            }
            for (int i=count;i<row.Pips.Count;i++) row.Pips[i].gameObject.SetActive(false);
        }

        private void UpdateSafeArea()
        {
            if (!safeArea || Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 size = new Vector2(Screen.width, Screen.height);
            Rect area = Screen.safeArea;
            if (area.width <= 0 || area.height <= 0) area = new Rect(0,0,size.x,size.y);
            if (lastScreen == size && lastSafeArea == area) return;
            safeArea.anchorMin = new Vector2(area.xMin/size.x,area.yMin/size.y);
            safeArea.anchorMax = new Vector2(area.xMax/size.x,area.yMax/size.y);
            lastScreen=size;lastSafeArea=area;
        }

        private RectTransform Rect(string name, Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;
        }
        private Image Picture(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var rect=Rect(name,parent);Place(rect,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        private TextMeshProUGUI Text(string name,Transform parent,float size,Vector2 position,Vector2 dimensions,Color color)
        {
            var rect=Rect(name,parent);Place(rect,position,dimensions);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            if(font)text.font=font;text.fontSize=size;text.color=color;text.text="";text.enableWordWrapping=false;
            text.overflowMode=TextOverflowModes.Ellipsis;text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;
            var shadow=rect.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);
            return text;
        }
    }
}
