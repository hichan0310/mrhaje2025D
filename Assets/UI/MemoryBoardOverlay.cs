using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayerSystem;
using InventoryItem = PlayerSystem.PlayerMemoryBinder.MemoryPieceInventoryItem;

namespace UI
{
    /// <summary>
    /// Controller that renders the player's memory board and inventory, allowing the player
    /// to arrange pieces visually.
    /// </summary>
    public class MemoryBoardOverlay : MonoBehaviour
    {
        private sealed class InventoryItemComparer : IEqualityComparer<InventoryItem>
        {
            public static readonly InventoryItemComparer Instance = new InventoryItemComparer();

            public bool Equals(InventoryItem x, InventoryItem y) => ItemsEqual(x, y);

            public int GetHashCode(InventoryItem obj)
            {
                int assetHash = obj.Asset ? obj.Asset.GetInstanceID() : 0;
                int multiplierHash = Mathf.RoundToInt(obj.PowerMultiplier * 1000f);
                return (assetHash * 397) ^ multiplierHash;
            }
        }

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private RectTransform boardGridRoot = null;
        [SerializeField] private MemoryBoardCellView cellPrefab = null;
        [SerializeField] private ScrollRect inventoryScrollRect = null;
        [SerializeField] private RectTransform inventoryContentRoot = null;
        [SerializeField] private VerticalLayoutGroup inventoryLayoutGroup = null;
        [SerializeField] private MemoryPieceInventoryItemView inventoryItemPrefab = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private TMP_Text selectedPieceLabel = null;

        [Header("Visual Settings")]
        [SerializeField] private Color emptyCellColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color occupiedCellColor = new Color(0.36f, 0.68f, 0.94f, 0.9f);
        [SerializeField] private Color originCellColor = new Color(0.94f, 0.74f, 0.36f, 0.95f);
        [SerializeField] private Color reinforcementColor = new Color(0.5f, 0.9f, 0.6f, 0.6f);
        [SerializeField] private float inventoryItemSpacing = 12f;
        [SerializeField] private float inventoryPaddingTop = 12f;
        [SerializeField] private float inventoryPaddingBottom = 12f;

        private readonly Dictionary<Vector2Int, MemoryBoardCellView> cellLookup = new();
        private readonly List<MemoryBoardCellView> cellViews = new();
        private readonly List<MemoryPieceInventoryItemView> inventoryViews = new();
        private readonly List<MemoryBoard.MemoryPiecePlacementInfo> placementBuffer = new();
        private readonly List<MemoryBoard.MemoryReinforcementInfo> reinforcementBuffer = new();
        private readonly Dictionary<Vector2Int, MemoryPieceAsset> cellOccupants = new();
        private readonly Dictionary<MemoryPieceAsset, MemoryBoard.MemoryPiecePlacementInfo> pieceLookup = new();

        private PlayerMemoryBinder boundBinder = null;
        private InventoryItem? selectedItem = null;

        private void Awake()
        {
            HideImmediate();
            if (closeButton)
            {
                closeButton.onClick.AddListener(Close);
            }

            ConfigureScrollRect();
            ConfigureInventoryLayoutGroup();
        }

        private void OnDestroy()
        {
            if (closeButton)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            DetachBinder();
        }

        public void Open(PlayerMemoryBinder binder)
        {
            if (!binder)
            {
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            AttachBinder(binder);
            Show();
        }

        public void Close()
        {
            Hide();
            DetachBinder();
        }

        private void AttachBinder(PlayerMemoryBinder binder)
        {
            if (boundBinder == binder)
            {
                RefreshAll();
                return;
            }

            DetachBinder();
            boundBinder = binder;
            boundBinder.InventoryChanged += HandleInventoryChanged;
            boundBinder.Board.OnPieceAdded += HandleBoardChanged;
            boundBinder.Board.OnPieceRemoved += HandleBoardChanged;

            RebuildBoardGrid();
            RefreshAll();
        }

        private void DetachBinder()
        {
            if (boundBinder == null)
            {
                return;
            }

            boundBinder.InventoryChanged -= HandleInventoryChanged;
            boundBinder.Board.OnPieceAdded -= HandleBoardChanged;
            boundBinder.Board.OnPieceRemoved -= HandleBoardChanged;
            boundBinder = null;
            selectedItem = null;
            UpdateSelectedPieceLabel();
        }

        private void HandleInventoryChanged()
        {
            RefreshInventory();
        }

        private void HandleBoardChanged(MemoryPieceAsset _)
        {
            RefreshBoard();
        }

        private void RefreshAll()
        {
            RefreshInventory();
            RefreshBoard();
        }

        private void RebuildBoardGrid()
        {
            foreach (var view in cellViews)
            {
                if (view)
                {
                    view.Clicked -= HandleCellClicked;
                    Destroy(view.gameObject);
                }
            }

            cellViews.Clear();
            cellLookup.Clear();

            if (!boundBinder)
            {
                return;
            }

            Vector2Int size = boundBinder.Board.GridSize;
            ConfigureBoardLayout(size);
            for (int y = size.y - 1; y >= 0; y--)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var cell = Instantiate(cellPrefab, boardGridRoot);
                    cell.Initialize(new Vector2Int(x, y));
                    cell.SetColors(emptyCellColor, occupiedCellColor, originCellColor, reinforcementColor);
                    cell.Clicked += HandleCellClicked;
                    cellViews.Add(cell);
                    cellLookup[cell.Coordinates] = cell;
                }
            }
        }

        private void ConfigureBoardLayout(Vector2Int size)
        {
            if (!boardGridRoot)
            {
                return;
            }

            var layout = boardGridRoot.GetComponent<GridLayoutGroup>();
            if (!layout)
            {
                return;
            }

            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Mathf.Max(1, size.x);
        }

        private void RefreshInventory()
        {
            ConfigureInventoryLayoutGroup();

            if (!boundBinder)
            {
                foreach (var view in inventoryViews)
                {
                    view.gameObject.SetActive(false);
                }
                selectedItem = null;
                UpdateSelectedPieceLabel();
                return;
            }

            var grouped = boundBinder.Inventory
                .Where(item => item.Asset)
                .GroupBy(item => item, InventoryItemComparer.Instance)
                .Select(group => new { Item = group.Key, Count = group.Count() })
                .OrderBy(group => group.Item.Asset.DisplayName)
                .ThenBy(group => group.Item.PowerMultiplier)
                .ToList();

            if (selectedItem.HasValue && !grouped.Any(g => ItemsEqual(g.Item, selectedItem.Value)))
            {
                selectedItem = null;
            }

            EnsureInventoryViewCount(grouped.Count);

            for (int i = 0; i < inventoryViews.Count; i++)
            {
                if (i < grouped.Count)
                {
                    var entry = grouped[i];
                    var view = inventoryViews[i];
                    view.gameObject.SetActive(true);
                    view.Bind(entry.Item.Asset, entry.Item.PowerMultiplier, entry.Count);
                    view.SetSelected(selectedItem.HasValue && ItemsEqual(selectedItem.Value, entry.Item));
                }
                else
                {
                    inventoryViews[i].gameObject.SetActive(false);
                }
            }

            UpdateSelectedPieceLabel();
        }

        private void EnsureInventoryViewCount(int count)
        {
            while (inventoryViews.Count < count)
            {
                var view = Instantiate(inventoryItemPrefab, inventoryContentRoot);
                view.Initialize(HandleInventoryItemClicked);
                view.Bind(null, 1f, 0);
                ConfigureInventoryItemRect(view.transform as RectTransform);
                inventoryViews.Add(view);
            }
        }

        private void RefreshBoard()
        {
            foreach (var cell in cellViews)
            {
                if (cell)
                {
                    cell.Clear();
                }
            }

            cellOccupants.Clear();
            pieceLookup.Clear();

            if (!boundBinder)
            {
                return;
            }

            boundBinder.Board.GetReinforcementPlacements(reinforcementBuffer);
            foreach (var info in reinforcementBuffer)
            {
                foreach (var cellPos in info.OccupiedCells)
                {
                    if (cellLookup.TryGetValue(cellPos, out var cell))
                    {
                        cell.SetReinforced(true, info.Zone ? info.Zone.BonusPercent : 0f);
                    }
                }
            }

            boundBinder.Board.GetPiecePlacements(placementBuffer);
            foreach (var placement in placementBuffer)
            {
                if (!placement.Asset)
                {
                    continue;
                }

                pieceLookup[placement.Asset] = placement;
                foreach (var cellPos in placement.OccupiedCells)
                {
                    if (!cellLookup.TryGetValue(cellPos, out var cell))
                    {
                        continue;
                    }

                    bool isOrigin = cellPos == placement.Origin;
                    cell.SetPiece(placement.Asset, placement.PowerMultiplier, placement.Locked, isOrigin);
                    cellOccupants[cellPos] = placement.Asset;
                }
            }
        }

        private void HandleInventoryItemClicked(MemoryPieceInventoryItemView view)
        {
            if (!view || !view.HasAsset)
            {
                return;
            }

            var item = new InventoryItem(view.Asset, view.Multiplier);
            if (selectedItem.HasValue && ItemsEqual(selectedItem.Value, item))
            {
                selectedItem = null;
            }
            else
            {
                selectedItem = item;
            }

            RefreshInventory();
        }

        private void HandleCellClicked(MemoryBoardCellView cell)
        {
            if (!boundBinder || cell == null)
            {
                return;
            }

            if (cellOccupants.TryGetValue(cell.Coordinates, out var occupant) && occupant)
            {
                if (pieceLookup.TryGetValue(occupant, out var placement) && placement.Locked)
                {
                    return;
                }

                boundBinder.RemovePiece(occupant);
                return;
            }

            if (!selectedItem.HasValue)
            {
                return;
            }

            var item = selectedItem.Value;
            bool placed = boundBinder.TryPlaceInventoryPiece(item, cell.Coordinates, false);
            if (placed && !boundBinder.HasInventoryPiece(item))
            {
                selectedItem = null;
                RefreshInventory();
            }
        }

        private void ConfigureScrollRect()
        {
            if (!inventoryScrollRect)
            {
                return;
            }

            inventoryScrollRect.horizontal = false;
            inventoryScrollRect.vertical = true;
            inventoryScrollRect.movementType = ScrollRect.MovementType.Clamped;

            if (inventoryContentRoot)
            {
                inventoryScrollRect.content = inventoryContentRoot;
            }
        }

        private void ConfigureInventoryLayoutGroup()
        {
            if (!inventoryContentRoot)
            {
                return;
            }

            if (!inventoryLayoutGroup)
            {
                inventoryLayoutGroup = inventoryContentRoot.GetComponent<VerticalLayoutGroup>();
            }

            if (inventoryLayoutGroup)
            {
                var padding = inventoryLayoutGroup.padding;
                padding.top = Mathf.RoundToInt(Mathf.Max(0f, inventoryPaddingTop));
                padding.bottom = Mathf.RoundToInt(Mathf.Max(0f, inventoryPaddingBottom));
                inventoryLayoutGroup.padding = padding;
                inventoryLayoutGroup.spacing = Mathf.Max(0f, inventoryItemSpacing);
                inventoryLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                inventoryLayoutGroup.childControlHeight = true;
                inventoryLayoutGroup.childControlWidth = true;
                inventoryLayoutGroup.childForceExpandHeight = false;
                inventoryLayoutGroup.childForceExpandWidth = true;
            }

            var fitter = inventoryContentRoot.GetComponent<ContentSizeFitter>();
            if (fitter)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (inventoryScrollRect && inventoryScrollRect.content != inventoryContentRoot)
            {
                inventoryScrollRect.content = inventoryContentRoot;
            }
        }

        private static void ConfigureInventoryItemRect(RectTransform rect)
        {
            if (!rect)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
            rect.localScale = Vector3.one;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ConfigureScrollRect();
            ConfigureInventoryLayoutGroup();

            if (!Application.isPlaying)
            {
                if (inventoryContentRoot)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(inventoryContentRoot);
                }
            }
        }
#endif

        private void Show()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
        }

        private void Hide()
        {
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void UpdateSelectedPieceLabel()
        {
            if (!selectedPieceLabel)
            {
                return;
            }

            if (selectedItem.HasValue)
            {
                var item = selectedItem.Value;
                string multiplierText = Mathf.Approximately(item.PowerMultiplier, 1f)
                    ? string.Empty
                    : $" ×{item.PowerMultiplier:0.##}";
                selectedPieceLabel.text = $"선택된 메모리: {item.Asset.DisplayName}{multiplierText}";
            }
            else
            {
                selectedPieceLabel.text = "선택된 메모리가 없습니다";
            }
        }

        private static bool ItemsEqual(InventoryItem a, InventoryItem b)
        {
            if (a.Asset != b.Asset)
            {
                return false;
            }

            return Mathf.Abs(a.PowerMultiplier - b.PowerMultiplier) < 0.001f;
        }
    }
}
