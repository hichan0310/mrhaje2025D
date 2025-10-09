using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontend
{
    /// <summary>
    /// BattleMapDefinition에 정의된 배틀 타일을 기반으로 2D 물리 Collider를 생성해
    /// 플레이어가 밟을 수 있는 실제 지형을 구성합니다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class BattlePlayfieldGenerator : MonoBehaviour
    {
        [Header("Map Source")]
        [SerializeField]
        [Tooltip("BattleUIController에서 맵 정의를 가져올 경우 연결합니다. 비워 두면 아래 정의를 사용합니다.")]
        private BattleUIController mapSource;

        [SerializeField]
        [Tooltip("mapSource가 비어 있거나 연결되지 않았을 때 사용할 로컬 맵 정의입니다.")]
        private BattleMapDefinition mapDefinition = BattleMapDefinition.Default();

        [SerializeField]
        [Tooltip("mapSource가 지정되어 있을 때에도 항상 이 맵 정의를 강제로 사용하려면 끄세요.")]
        private bool inheritDefinitionFromSource = true;

        [Header("Placement")]
        [SerializeField]
        [Tooltip("생성된 Collider를 배치할 부모 Transform입니다. 비워 두면 컴포넌트가 있는 오브젝트 아래에 자동으로 생성됩니다.")]
        private Transform playfieldRoot;

        [SerializeField]
        [Tooltip("타일 (0,0)의 월드 기준 위치입니다.")]
        private Vector2 playfieldOrigin = Vector2.zero;

        [SerializeField]
        [Tooltip("타일 하나가 차지하는 월드 단위 크기입니다.")]
        private float tileWorldSize = 1f;

        [SerializeField]
        [Tooltip("Collider를 축소하기 위한 여백입니다. 값이 커질수록 Collider가 작아집니다.")]
        private Vector2 colliderPadding = new Vector2(0.05f, 0.05f);

        [SerializeField]
        [Tooltip("PlatformEffector2D 표면 각도입니다. Platform 규칙에만 적용됩니다.")]
        private float platformSurfaceArc = 160f;

        [Header("Visualization")]
        [SerializeField]
        [Tooltip("Collider 위치를 확인하기 위한 디버그 SpriteRenderer를 추가합니다.")]
        private bool addDebugSprites = true;

        [SerializeField]
        private Color groundTileColor = new Color(0.18f, 0.72f, 0.43f, 0.75f);

        [SerializeField]
        private Color platformTileColor = new Color(0.25f, 0.52f, 0.93f, 0.75f);

        [Header("Tile Rules")]
        [SerializeField]
        private List<PlayfieldTileRule> tileRules = new List<PlayfieldTileRule>
        {
            new PlayfieldTileRule
            {
                tileType = BattleTileType.Obstacle,
                collision = PlayfieldCollisionType.Solid,
                layerName = "Ground",
                tag = "Ground",
            },
            new PlayfieldTileRule
            {
                tileType = BattleTileType.PlayerSpawn,
                collision = PlayfieldCollisionType.Platform,
                layerName = "Platform",
                tag = "Platform",
            },
            new PlayfieldTileRule
            {
                tileType = BattleTileType.EnemySpawn,
                collision = PlayfieldCollisionType.Platform,
                layerName = "Platform",
                tag = "Platform",
            },
        };

        private readonly List<GameObject> generatedTiles = new List<GameObject>();
        private readonly HashSet<string> warnedLayers = new HashSet<string>();
        private readonly HashSet<string> warnedTags = new HashSet<string>();
        private Transform runtimeRoot;
        private int lastDefinitionHash;
        private static Sprite fallbackDebugSprite;

        private void OnEnable()
        {
            lastDefinitionHash = 0;
            RebuildIfNeeded(force: true);
        }

        private void OnDisable()
        {
            ClearGeneratedTiles();
        }

        private void OnValidate()
        {
            mapDefinition ??= BattleMapDefinition.Default();
            mapDefinition.EnsureValid();
            tileWorldSize = Mathf.Max(0.01f, tileWorldSize);
            colliderPadding.x = Mathf.Clamp(colliderPadding.x, 0f, tileWorldSize * 0.9f);
            colliderPadding.y = Mathf.Clamp(colliderPadding.y, 0f, tileWorldSize * 0.9f);
            platformSurfaceArc = Mathf.Clamp(platformSurfaceArc, 0f, 360f);

            if (tileRules == null)
            {
                tileRules = new List<PlayfieldTileRule>();
            }

            RebuildIfNeeded(force: true);
        }

        private void Update()
        {
            RebuildIfNeeded();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var definition = GetActiveDefinition();
            if (definition == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            var origin = new Vector3(playfieldOrigin.x, playfieldOrigin.y, 0f);
            var width = definition.Width * tileWorldSize;
            var height = definition.Height * tileWorldSize;
            Gizmos.DrawWireCube(origin + new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(width, height, 0f));
        }
#endif

        [ContextMenu("Rebuild Playfield Now")]
        public void RebuildPlayfield()
        {
            RebuildImmediate();
        }

        private void RebuildIfNeeded(bool force = false)
        {
            var definition = GetActiveDefinition();
            var hash = ComputeDefinitionHash(definition);

            if (!force && hash == lastDefinitionHash)
            {
                return;
            }

            RebuildImmediate(definition, hash);
        }

        private void RebuildImmediate(BattleMapDefinition definition = null, int precomputedHash = 0)
        {
            definition ??= GetActiveDefinition();
            if (definition == null)
            {
                ClearGeneratedTiles();
                lastDefinitionHash = 0;
                return;
            }

            definition.EnsureValid();
            ClearGeneratedTiles();

            var root = ResolveRoot();
            warnedLayers.Clear();
            warnedTags.Clear();

            for (var y = 0; y < definition.Height; y++)
            {
                for (var x = 0; x < definition.Width; x++)
                {
                    var tileType = definition.GetTile(x, y);
                    if (!TryGetRule(tileType, out var rule) || rule.collision == PlayfieldCollisionType.None)
                    {
                        continue;
                    }

                    var tileObject = new GameObject($"Playfield_{tileType}_{x}_{y}");
                    tileObject.transform.SetParent(root, false);
                    var localCenter = new Vector3(
                        playfieldOrigin.x + (x + 0.5f) * tileWorldSize,
                        playfieldOrigin.y + (y + 0.5f) * tileWorldSize,
                        0f);
                    tileObject.transform.localPosition = localCenter;

                    AssignLayer(tileObject, rule.layerName);
                    AssignTag(tileObject, rule.tag);

                    var collider = tileObject.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(
                        Mathf.Max(0.01f, tileWorldSize - colliderPadding.x),
                        Mathf.Max(0.01f, tileWorldSize - colliderPadding.y));
                    collider.offset = Vector2.zero;
                    collider.isTrigger = false;
                    collider.usedByEffector = rule.collision == PlayfieldCollisionType.Platform;

                    if (rule.collision == PlayfieldCollisionType.Platform)
                    {
                        var effector = tileObject.AddComponent<PlatformEffector2D>();
                        effector.useOneWay = true;
                        effector.surfaceArc = platformSurfaceArc;
                        effector.useOneWayGrouping = false;
                    }

                    if (addDebugSprites)
                    {
                        var spriteRenderer = tileObject.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = GetDebugSprite();
                        spriteRenderer.color = rule.collision == PlayfieldCollisionType.Solid ? groundTileColor : platformTileColor;
                        spriteRenderer.sortingOrder = -50;
                        spriteRenderer.drawMode = SpriteDrawMode.Simple;
                        spriteRenderer.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1f);
                    }

                    generatedTiles.Add(tileObject);
                }
            }

            lastDefinitionHash = precomputedHash != 0 ? precomputedHash : ComputeDefinitionHash(definition);
        }

        private void ClearGeneratedTiles()
        {
            for (var i = generatedTiles.Count - 1; i >= 0; i--)
            {
                var tile = generatedTiles[i];
                if (tile == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(tile);
                }
                else
                {
                    DestroyImmediate(tile);
                }
            }

            generatedTiles.Clear();

            if (!Application.isPlaying && runtimeRoot != null && runtimeRoot.childCount == 0)
            {
                if (runtimeRoot != transform)
                {
                    DestroyImmediate(runtimeRoot.gameObject);
                }

                runtimeRoot = null;
            }
        }

        private Transform ResolveRoot()
        {
            if (playfieldRoot != null)
            {
                return playfieldRoot;
            }

            if (runtimeRoot == null)
            {
                var go = new GameObject("GeneratedPlayfield");
                runtimeRoot = go.transform;
                runtimeRoot.SetParent(transform, false);
                runtimeRoot.localPosition = Vector3.zero;
            }

            return runtimeRoot;
        }

        private BattleMapDefinition GetActiveDefinition()
        {
            if (inheritDefinitionFromSource && mapSource != null)
            {
                var sourceDefinition = mapSource.CurrentMapDefinition;
                if (sourceDefinition != null)
                {
                    sourceDefinition.EnsureValid();
                    return sourceDefinition;
                }
            }

            mapDefinition ??= BattleMapDefinition.Default();
            mapDefinition.EnsureValid();
            return mapDefinition;
        }

        private bool TryGetRule(BattleTileType tileType, out PlayfieldTileRule rule)
        {
            if (tileRules != null)
            {
                for (var i = 0; i < tileRules.Count; i++)
                {
                    if (tileRules[i].tileType == tileType)
                    {
                        rule = tileRules[i];
                        return true;
                    }
                }
            }

            rule = default;
            return false;
        }

        private void AssignLayer(GameObject tileObject, string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
            {
                return;
            }

            var layerIndex = LayerMask.NameToLayer(layerName);
            if (layerIndex < 0)
            {
                if (warnedLayers.Add(layerName))
                {
                    Debug.LogWarning($"[BattlePlayfieldGenerator] 지정한 레이어 '{layerName}'가 존재하지 않습니다. 기본 레이어를 사용합니다.", this);
                }

                return;
            }

            tileObject.layer = layerIndex;
        }

        private void AssignTag(GameObject tileObject, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || string.Equals(tag, "Untagged", StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                tileObject.tag = tag;
            }
            catch (UnityException)
            {
                if (warnedTags.Add(tag))
                {
                    Debug.LogWarning($"[BattlePlayfieldGenerator] 지정한 태그 '{tag}'를 찾을 수 없습니다. 기본 태그를 유지합니다.", this);
                }
            }
        }

        private static Sprite GetDebugSprite()
        {
            if (fallbackDebugSprite != null)
            {
                return fallbackDebugSprite;
            }

            var texture = Texture2D.whiteTexture;
            fallbackDebugSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            fallbackDebugSprite.name = "BattlePlayfieldDebugSprite";
            return fallbackDebugSprite;
        }

        private int ComputeDefinitionHash(BattleMapDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            var hash = (definition.Width * 397) ^ definition.Height;
            for (var y = 0; y < definition.Height; y++)
            {
                for (var x = 0; x < definition.Width; x++)
                {
                    hash = (hash * 31) + (int)definition.GetTile(x, y);
                }
            }

            return hash;
        }

        [Serializable]
        private struct PlayfieldTileRule
        {
            public BattleTileType tileType;
            public PlayfieldCollisionType collision;
            public string layerName;
            public string tag;
        }

        private enum PlayfieldCollisionType
        {
            None = 0,
            Solid = 1,
            Platform = 2,
        }
    }
}
