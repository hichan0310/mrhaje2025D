using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frontend
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class BattleMapRenderer : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField]
        private float cellSize = 80f;

        [SerializeField]
        private Vector2 cellSpacing = new Vector2(12f, 12f);

        [SerializeField]
        private Sprite tileSprite;

        [Header("Colors")]
        [SerializeField]
        private Color baseTileColor = new Color(0.15f, 0.18f, 0.22f, 1f);

        [SerializeField]
        private Color obstacleColor = new Color(0.45f, 0.21f, 0.21f, 1f);

        [SerializeField]
        private Color playerSpawnColor = new Color(0.17f, 0.55f, 0.36f, 1f);

        [SerializeField]
        private Color enemySpawnColor = new Color(0.55f, 0.2f, 0.42f, 1f);

        [SerializeField]
        private Color outlineColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        [SerializeField]
        private float outlineThickness = 2f;

        [Header("Tags & Layers")]
        [SerializeField]
        [Tooltip("생성되는 기본 타일에 적용할 Unity 태그. 비워두면 변경하지 않습니다.")]
        private string defaultTileTag;

        [SerializeField]
        private string obstacleTag;

        [SerializeField]
        private string playerSpawnTag;

        [SerializeField]
        private string enemySpawnTag;

        [SerializeField]
        [Tooltip("생성되는 기본 타일에 적용할 Unity 레이어 이름. 비워두면 변경하지 않습니다.")]
        private string defaultTileLayer;

        [SerializeField]
        private string obstacleLayer;

        [SerializeField]
        private string playerSpawnLayer;

        [SerializeField]
        private string enemySpawnLayer;

        [Header("Interaction")]
        [SerializeField]
        [Tooltip("UI 그래픽 레이캐스트로 타일을 선택할 수 있게 Image.raycastTarget을 활성화합니다. UI 전용 맵이므로 별도의 Collider가 필요하지 않습니다.")]
        private bool enableTileRaycasts = true;

        [Header("Debug")]
        [SerializeField]
        private bool showCoordinates;

        private GridLayoutGroup gridLayout;

        private void Awake()
        {
            ConfigureGrid();
        }

        private void OnValidate()
        {
            ConfigureGrid();

            if (gridLayout != null)
            {
                gridLayout.cellSize = new Vector2(Mathf.Max(16f, cellSize), Mathf.Max(16f, cellSize));
                gridLayout.spacing = new Vector2(Mathf.Max(0f, cellSpacing.x), Mathf.Max(0f, cellSpacing.y));
            }
        }

        public void Render(BattleMapDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("BattleMapRenderer.Render received a null definition.", this);
                return;
            }

            ConfigureGrid();
            definition.EnsureValid();

            ClearChildren();

            var width = Mathf.Max(1, definition.Width);
            var height = Mathf.Max(1, definition.Height);

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = width;
            var clampedCellSize = Mathf.Max(16f, cellSize);
            var spacingX = Mathf.Max(0f, cellSpacing.x);
            var spacingY = Mathf.Max(0f, cellSpacing.y);
            gridLayout.cellSize = new Vector2(clampedCellSize, clampedCellSize);
            gridLayout.spacing = new Vector2(spacingX, spacingY);
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = 0; x < width; x++)
                {
                    var tileType = definition.GetTile(x, y);
                    CreateTile(x, y, tileType);
                }
            }
        }

        private void ConfigureGrid()
        {
            if (gridLayout == null)
            {
                gridLayout = GetComponent<GridLayoutGroup>();
                if (gridLayout == null)
                {
                    gridLayout = gameObject.AddComponent<GridLayoutGroup>();
                }
            }

            gridLayout.enabled = true;
            gridLayout.padding = new RectOffset();
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 1;
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void CreateTile(int x, int y, BattleTileType type)
        {
            var tileObject = new GameObject($"Tile_{x}_{y}", typeof(RectTransform));
            tileObject.transform.SetParent(transform, false);

            var rect = (RectTransform)tileObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = tileObject.AddComponent<Image>();
            image.sprite = tileSprite;
            image.type = tileSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = ResolveColor(type);
            image.raycastTarget = enableTileRaycasts;

            ApplyTagAndLayer(tileObject, type);

            if (outlineThickness > 0f)
            {
                var outline = tileObject.AddComponent<Outline>();
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(outlineThickness, -outlineThickness);
                outline.useGraphicAlpha = false;
            }

            if (showCoordinates || type == BattleTileType.PlayerSpawn || type == BattleTileType.EnemySpawn)
            {
                var label = CreateLabel(tileObject.transform);

                if (showCoordinates)
                {
                    label.text = type switch
                    {
                        BattleTileType.PlayerSpawn => $"P\n({x}, {y})",
                        BattleTileType.EnemySpawn => $"E\n({x}, {y})",
                        _ => $"{x}, {y}",
                    };
                }
                else if (type == BattleTileType.PlayerSpawn || type == BattleTileType.EnemySpawn)
                {
                    label.text = type == BattleTileType.PlayerSpawn ? "P" : "E";
                }
            }
        }

        private TextMeshProUGUI CreateLabel(Transform parent)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 22f;
            text.enableWordWrapping = false;
            text.color = new Color(0.93f, 0.95f, 0.98f, 0.95f);

            return text;
        }

        private Color ResolveColor(BattleTileType type)
        {
            return type switch
            {
                BattleTileType.Obstacle => obstacleColor,
                BattleTileType.PlayerSpawn => playerSpawnColor,
                BattleTileType.EnemySpawn => enemySpawnColor,
                _ => baseTileColor,
            };
        }

        private void ApplyTagAndLayer(GameObject tileObject, BattleTileType type)
        {
            var tagName = ResolveTag(type);
            if (!string.IsNullOrWhiteSpace(tagName))
            {
                try
                {
                    tileObject.tag = tagName;
                }
                catch (UnityException)
                {
                    Debug.LogWarning($"BattleMapRenderer: '{tagName}' 태그가 정의되어 있지 않아 타일에 적용하지 못했습니다.", this);
                }
            }

            var layerName = ResolveLayer(type);
            if (!string.IsNullOrWhiteSpace(layerName))
            {
                var layer = LayerMask.NameToLayer(layerName);
                if (layer == -1)
                {
                    Debug.LogWarning($"BattleMapRenderer: '{layerName}' 레이어가 존재하지 않아 타일에 적용하지 못했습니다.", this);
                }
                else
                {
                    tileObject.layer = layer;
                }
            }
        }

        private string ResolveTag(BattleTileType type)
        {
            return type switch
            {
                BattleTileType.Obstacle => obstacleTag,
                BattleTileType.PlayerSpawn => playerSpawnTag,
                BattleTileType.EnemySpawn => enemySpawnTag,
                _ => defaultTileTag,
            };
        }

        private string ResolveLayer(BattleTileType type)
        {
            return type switch
            {
                BattleTileType.Obstacle => obstacleLayer,
                BattleTileType.PlayerSpawn => playerSpawnLayer,
                BattleTileType.EnemySpawn => enemySpawnLayer,
                _ => defaultTileLayer,
            };
        }
    }
}
