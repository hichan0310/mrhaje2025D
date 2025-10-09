using System.Collections.Generic;
using EntitySystem;
using UnityEngine;
using UnityEngine.UI;

namespace Frontend
{
    [DisallowMultipleComponent]
    public class BattleUIController : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField]
        private BattleMapDefinition mapDefinition = BattleMapDefinition.Default();

        [SerializeField]
        private Vector2 mapSizeInPixels = new Vector2(640f, 400f);

        [SerializeField]
        private Vector2 mapAnchorOffset = new Vector2(0f, 120f);

        [Header("HUD")]
        [SerializeField]
        private Vector2 hudAnchorPadding = new Vector2(24f, 24f);

        [SerializeField]
        private Vector2 hudPreferredSize = new Vector2(320f, 72f);

        [SerializeField]
        private float hudSpacing = 12f;

        [Header("Canvas")]
        [SerializeField]
        [Tooltip("Battle UI 캔버스의 RenderMode입니다. 기본적으로 ScreenSpaceOverlay로 설정되어 월드 오브젝트와 겹치지 않습니다.")]
        private RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;

        [SerializeField]
        private Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [SerializeField]
        [Range(0f, 1f)]
        private float referenceMatch = 0.5f;

        [Header("Entities")]
        [SerializeField]
        private Entity playerEntity;

        [SerializeField]
        private string playerLabel = "Player";

        [SerializeField]
        private List<Entity> enemyEntities = new List<Entity>();

        [SerializeField]
        private string enemyLabelPrefix = "Enemy";

        private Canvas canvas;
        private BattleMapRenderer mapRenderer;
        private RectTransform hudContainer;
        private readonly List<EntityHealthView> healthViews = new List<EntityHealthView>();

        private void Reset()
        {
            mapDefinition = BattleMapDefinition.Default();
        }

        private void OnValidate()
        {
            mapDefinition ??= BattleMapDefinition.Default();
            mapDefinition.EnsureValid();
            mapSizeInPixels.x = Mathf.Max(64f, mapSizeInPixels.x);
            mapSizeInPixels.y = Mathf.Max(64f, mapSizeInPixels.y);
            hudPreferredSize.x = Mathf.Max(120f, hudPreferredSize.x);
            hudPreferredSize.y = Mathf.Max(48f, hudPreferredSize.y);
            hudSpacing = Mathf.Max(0f, hudSpacing);
            referenceResolution.x = Mathf.Max(320f, referenceResolution.x);
            referenceResolution.y = Mathf.Max(240f, referenceResolution.y);
            referenceMatch = Mathf.Clamp01(referenceMatch);
        }

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }
            else
            {
                ClearCanvasChildren();
            }

            mapDefinition ??= BattleMapDefinition.Default();
            mapDefinition.EnsureValid();
            enemyEntities ??= new List<Entity>();

            BuildMapArea();
            BuildHud();
        }

        private Canvas CreateCanvas()
        {
            var canvasGo = new GameObject("BattleUI_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var createdCanvas = canvasGo.GetComponent<Canvas>();
            createdCanvas.renderMode = canvasRenderMode;
            createdCanvas.pixelPerfect = false;

            if (canvasRenderMode == RenderMode.ScreenSpaceCamera && createdCanvas.worldCamera == null)
            {
                createdCanvas.worldCamera = Camera.main;
            }

            if (canvasRenderMode == RenderMode.WorldSpace)
            {
                var rect = (RectTransform)createdCanvas.transform;
                rect.sizeDelta = referenceResolution;
            }

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = referenceMatch;

            return createdCanvas;
        }

        private void ClearCanvasChildren()
        {
            if (canvas == null)
            {
                return;
            }

            for (var i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            healthViews.Clear();
            hudContainer = null;
            mapRenderer = null;
        }

        private void BuildMapArea()
        {
            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            var mapRoot = CreateRectTransform("Map", canvasRect);
            mapRoot.anchorMin = new Vector2(0.5f, 0.5f);
            mapRoot.anchorMax = new Vector2(0.5f, 0.5f);
            mapRoot.pivot = new Vector2(0.5f, 0.5f);
            var clampedSize = new Vector2(Mathf.Max(64f, mapSizeInPixels.x), Mathf.Max(64f, mapSizeInPixels.y));
            mapRoot.sizeDelta = clampedSize;
            mapRoot.anchoredPosition = mapAnchorOffset;

            mapRenderer = mapRoot.gameObject.AddComponent<BattleMapRenderer>();
            mapRenderer.Render(mapDefinition);
        }

        private void BuildHud()
        {
            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            hudContainer = CreateRectTransform("HUD", canvasRect);
            hudContainer.anchorMin = new Vector2(0f, 0f);
            hudContainer.anchorMax = new Vector2(0f, 0f);
            hudContainer.pivot = new Vector2(0f, 0f);
            hudContainer.anchoredPosition = hudAnchorPadding;

            var layout = hudContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = hudSpacing;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            healthViews.Clear();

            if (playerEntity != null)
            {
                var playerHud = CreateHealthView(hudContainer, playerLabel);
                playerHud.Bind(playerEntity, playerLabel);
                healthViews.Add(playerHud);
            }

            for (var i = 0; i < enemyEntities.Count; i++)
            {
                var enemy = enemyEntities[i];
                if (enemy == null)
                {
                    continue;
                }

                var label = BuildEnemyLabel(enemy, i);
                var view = CreateHealthView(hudContainer, label);
                view.Bind(enemy, label);
                healthViews.Add(view);
            }
        }

        private EntityHealthView CreateHealthView(RectTransform parent, string label)
        {
            var element = new GameObject($"HUD_{label}", typeof(RectTransform), typeof(LayoutElement), typeof(EntityHealthView));
            element.transform.SetParent(parent, false);

            var layoutElement = element.GetComponent<LayoutElement>();
            layoutElement.minWidth = hudPreferredSize.x;
            layoutElement.preferredWidth = hudPreferredSize.x;
            layoutElement.minHeight = hudPreferredSize.y;
            layoutElement.preferredHeight = hudPreferredSize.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            var view = element.GetComponent<EntityHealthView>();
            view.SetDisplayName(label);
            return view;
        }

        private string BuildEnemyLabel(Entity enemy, int index)
        {
            if (!string.IsNullOrEmpty(enemyLabelPrefix))
            {
                return $"{enemyLabelPrefix} {index + 1}";
            }

            return string.IsNullOrWhiteSpace(enemy.name) ? $"Enemy {index + 1}" : enemy.name;
        }

        private RectTransform CreateRectTransform(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
    }
}
