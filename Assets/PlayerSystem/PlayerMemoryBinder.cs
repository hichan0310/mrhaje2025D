using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using GameBackend;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem
{
    /// <summary>
    /// Component that wires serialized memory boards to runtime player actions.
    /// </summary>
    public class PlayerMemoryBinder : MonoBehaviour, IEntityEventListener
    {
        [Serializable]
        private class StartingInventoryEntry
        {
            [SerializeField] internal MemoryPieceAsset piece = null;
            [SerializeField] [Range(0.1f, 10f)] internal float powerMultiplier = 1f;
        }

        [Serializable]
        private class TriggerBoardEntry
        {
            [SerializeField] internal ActionTriggerType trigger = ActionTriggerType.BasicAttack;
            [SerializeField] internal MemoryBoard board = new();
        }

        public readonly struct MemoryPieceInventoryItem
        {
            public MemoryPieceAsset Asset { get; }
            public float PowerMultiplier { get; }

            internal MemoryPieceInventoryItem(MemoryPieceAsset asset, float multiplier)
            {
                Asset = asset;
                PowerMultiplier = Mathf.Approximately(multiplier, 0f) ? 1f : Mathf.Max(0.1f, multiplier);
            }
        }

        [SerializeField] private Player player = null;
        [SerializeField] private List<TriggerBoardEntry> triggerBoards = new();
        [SerializeField] private float globalPowerScale = 1f;
        [SerializeField] private List<StartingInventoryEntry> startingInventory = new();

        public IReadOnlyList<MemoryPieceInventoryItem> Inventory => inventoryPieces;
        public IReadOnlyList<ActionTriggerType> AvailableTriggers => triggerOrder;
        public ActionTriggerType ActiveTrigger => activeTrigger;
        public MemoryBoard? ActiveBoard => TryGetBoard(activeTrigger, out var board) ? board : null;
        internal MemoryTriggerContext? CurrentContext => currentContext;

        public event Action? InventoryChanged;
        public event Action<ActionTriggerType>? BoardChanged;
        public event Action<ActionTriggerType>? ActiveBoardChanged;

        private readonly List<MemoryPieceInventoryItem> inventoryPieces = new();
        private readonly Dictionary<ActionTriggerType, MemoryBoard> boardLookup = new();
        private readonly Dictionary<ActionTriggerType, Action<MemoryPieceAsset>> boardAddHandlers = new();
        private readonly Dictionary<ActionTriggerType, Action<MemoryPieceAsset>> boardRemoveHandlers = new();
        private readonly List<ActionTriggerType> triggerOrder = new();
        private readonly Dictionary<MemoryPieceAsset, ActionTriggerType> pieceOwnership = new();
        private readonly List<MemoryBoard.MemoryPiecePlacementInfo> placementBuffer = new();

        private MemoryTriggerContext? currentContext = null;
        private MemoryTriggerContext? pendingContextCompletion = null;
        private ActionTriggerType activeTrigger = ActionTriggerType.None;

        private void Awake()
        {
            if (!player)
            {
                player = GetComponent<Player>();
            }

            BuildBoards();
            InitializeInventory();
            this.registerTarget(this.player);
        }

        private void OnDisable()
        {
            CompleteCurrentContext(force: true);
        }

        private void OnDestroy()
        {
            CompleteCurrentContext(force: true);
            foreach (var pair in boardLookup)
            {
                if (boardAddHandlers.TryGetValue(pair.Key, out var addHandler))
                {
                    pair.Value.OnPieceAdded -= addHandler;
                }

                if (boardRemoveHandlers.TryGetValue(pair.Key, out var removeHandler))
                {
                    pair.Value.OnPieceRemoved -= removeHandler;
                }
            }

            boardLookup.Clear();
            boardAddHandlers.Clear();
            boardRemoveHandlers.Clear();
            triggerOrder.Clear();
            pieceOwnership.Clear();
        }

        private void Update()
        {
            // IEventListener이기 때문에 필요 없습니다. update는 주 타겟에 의존하여 돌아가게 됩니다. 
        }

        private void LateUpdate()
        {
            CompleteCurrentContext();
        }

        public bool TryGetBoard(ActionTriggerType trigger, out MemoryBoard board)
        {
            trigger = NormalizeTrigger(trigger);
            return boardLookup.TryGetValue(trigger, out board);
        }

        public bool SetActiveBoard(ActionTriggerType trigger)
        {
            trigger = NormalizeTrigger(trigger);
            if (!boardLookup.ContainsKey(trigger))
            {
                return false;
            }

            if (activeTrigger == trigger)
            {
                return true;
            }

            activeTrigger = trigger;
            ActiveBoardChanged?.Invoke(activeTrigger);
            return true;
        }

        public void Trigger(ActionTriggerType triggerType, float basePower = 1f)
        {
            CompleteCurrentContext();

            if (!player)
            {
                return;
            }

            triggerType = NormalizeTrigger(triggerType);
            if (!boardLookup.TryGetValue(triggerType, out var board))
            {
                return;
            }

            float power = Mathf.Max(0f, basePower * globalPowerScale);
            var context = new MemoryTriggerContext(this, triggerType, board, power);
            currentContext = context;
            board.Trigger(triggerType, player, power, context);
            pendingContextCompletion = context;
        }

        public bool TryPlaceInventoryPiece(ActionTriggerType trigger, MemoryPieceInventoryItem item, Vector2Int origin,
            bool locked = false, int rotationSteps = 0)
        {
            trigger = NormalizeTrigger(trigger);
            if (!boardLookup.TryGetValue(trigger, out var board))
            {
                return false;
            }

            if (!item.Asset || !item.Asset.IsTriggerAllowed(trigger))
            {
                return false;
            }

            if (pieceOwnership.ContainsKey(item.Asset))
            {
                return false;
            }

            int index = FindInventoryIndex(item.Asset, item.PowerMultiplier, true);
            if (index < 0)
            {
                return false;
            }

            var entry = inventoryPieces[index];
            if (!board.TryAddPiece(entry.Asset, origin, entry.PowerMultiplier, locked, rotationSteps))
            {
                return false;
            }

            inventoryPieces.RemoveAt(index);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool CanPlacePiece(ActionTriggerType trigger, MemoryPieceAsset asset, Vector2Int origin,
            int rotationSteps = 0)
        {
            trigger = NormalizeTrigger(trigger);
            if (!asset || !boardLookup.TryGetValue(trigger, out var board))
            {
                return false;
            }

            return board.CanPlacePiece(asset, origin, rotationSteps);
        }

        public bool CanPlacePiece(MemoryPieceAsset asset, Vector2Int origin, int rotationSteps = 0)
        {
            return CanPlacePiece(activeTrigger, asset, origin, rotationSteps);
        }

        public bool TryPlaceInventoryPiece(MemoryPieceInventoryItem item, Vector2Int origin, bool locked = false,
            int rotationSteps = 0)
        {
            return TryPlaceInventoryPiece(activeTrigger, item, origin, locked, rotationSteps);
        }

        public bool RemovePiece(ActionTriggerType trigger, MemoryPieceAsset asset)
        {
            trigger = NormalizeTrigger(trigger);
            if (!asset)
            {
                return false;
            }

            if (!boardLookup.TryGetValue(trigger, out var board))
            {
                return false;
            }

            if (!board.TryGetPlacement(asset, out var placement))
            {
                return false;
            }

            if (!board.RemovePiece(asset))
            {
                return false;
            }

            inventoryPieces.Add(new MemoryPieceInventoryItem(placement.Asset, placement.PowerMultiplier));
            InventoryChanged?.Invoke();
            return true;
        }

        public bool RemovePiece(MemoryPieceAsset asset)
        {
            if (!asset)
            {
                return false;
            }

            if (!pieceOwnership.TryGetValue(asset, out var trigger))
            {
                return false;
            }

            return RemovePiece(trigger, asset);
        }

        public bool TryAddPieceToInventory(MemoryPieceAsset asset, float multiplier = 1f)
        {
            if (!asset)
            {
                return false;
            }

            inventoryPieces.Add(new MemoryPieceInventoryItem(asset, multiplier));
            InventoryChanged?.Invoke();
            return true;
        }

        public bool HasInventoryPiece(MemoryPieceInventoryItem item)
        {
            return FindInventoryIndex(item.Asset, item.PowerMultiplier, true) >= 0;
        }

        public bool ContainsPiece(MemoryPieceAsset asset)
        {
            return asset && pieceOwnership.ContainsKey(asset);
        }

        internal bool TryGetContext(out MemoryTriggerContext context)
        {
            if (currentContext != null)
            {
                context = currentContext;
                return true;
            }

            context = null;
            return false;
        }

        private void BuildBoards()
        {
            boardLookup.Clear();
            triggerOrder.Clear();
            pieceOwnership.Clear();
            boardAddHandlers.Clear();
            boardRemoveHandlers.Clear();

            foreach (var entry in triggerBoards)
            {
                if (entry == null || entry.board == null)
                {
                    continue;
                }

                ActionTriggerType trigger = NormalizeTrigger(entry.trigger);
                if (trigger == ActionTriggerType.None || boardLookup.ContainsKey(trigger))
                {
                    continue;
                }

                entry.board.Initialize(this, trigger);
                boardLookup[trigger] = entry.board;
                triggerOrder.Add(trigger);

                Action<MemoryPieceAsset> addedHandler = asset => HandleBoardPieceAdded(trigger, asset);
                Action<MemoryPieceAsset> removedHandler = asset => HandleBoardPieceRemoved(trigger, asset);
                boardAddHandlers[trigger] = addedHandler;
                boardRemoveHandlers[trigger] = removedHandler;
                entry.board.OnPieceAdded += addedHandler;
                entry.board.OnPieceRemoved += removedHandler;
            }

            placementBuffer.Clear();
            foreach (var pair in boardLookup)
            {
                pair.Value.GetPiecePlacements(placementBuffer);
                foreach (var placement in placementBuffer)
                {
                    if (placement.Asset)
                    {
                        pieceOwnership[placement.Asset] = pair.Key;
                    }
                }
            }

            if (triggerOrder.Count > 0)
            {
                activeTrigger = triggerOrder[0];
            }
            else
            {
                activeTrigger = ActionTriggerType.None;
            }
        }

        private void InitializeInventory()
        {
            inventoryPieces.Clear();
            foreach (var entry in startingInventory)
            {
                if (entry == null || !entry.piece)
                {
                    continue;
                }

                inventoryPieces.Add(new MemoryPieceInventoryItem(entry.piece, entry.powerMultiplier));
            }

            if (inventoryPieces.Count > 0)
            {
                InventoryChanged?.Invoke();
            }
        }

        private void HandleBoardPieceAdded(ActionTriggerType trigger, MemoryPieceAsset asset)
        {
            if (asset)
            {
                pieceOwnership[asset] = trigger;
            }

            BoardChanged?.Invoke(trigger);
        }

        private void HandleBoardPieceRemoved(ActionTriggerType trigger, MemoryPieceAsset asset)
        {
            if (asset)
            {
                pieceOwnership.Remove(asset);
            }

            BoardChanged?.Invoke(trigger);
        }

        private int FindInventoryIndex(MemoryPieceAsset asset, float multiplier, bool strictMultiplier)
        {
            if (!asset)
            {
                return -1;
            }

            const float epsilon = 0.001f;
            for (int i = 0; i < inventoryPieces.Count; i++)
            {
                var entry = inventoryPieces[i];
                if (entry.Asset != asset)
                {
                    continue;
                }

                if (!strictMultiplier || Mathf.Abs(entry.PowerMultiplier - multiplier) < epsilon)
                {
                    return i;
                }
            }

            return -1;
        }

        private static ActionTriggerType NormalizeTrigger(ActionTriggerType trigger)
        {
            if (trigger == ActionTriggerType.None)
            {
                return ActionTriggerType.None;
            }

            foreach (ActionTriggerType value in Enum.GetValues(typeof(ActionTriggerType)))
            {
                if (value == ActionTriggerType.None)
                {
                    continue;
                }

                if (trigger.HasFlag(value))
                {
                    return value;
                }
            }

            return ActionTriggerType.None;
        }

        private void CompleteCurrentContext(bool force = false)
        {
            if (pendingContextCompletion != null)
            {
                var context = pendingContextCompletion;
                pendingContextCompletion = null;
                context.Complete();

                if (ReferenceEquals(currentContext, context))
                {
                    currentContext = null;
                }

                return;
            }

            if (force && currentContext != null)
            {
                currentContext.Complete();
                currentContext = null;
            }
        }

        public void eventActive(EventArgs eventArgs)
        {
            foreach (var board in boardLookup.Values)
            {
                board.recieveEvent(eventArgs);
            }
        }

        public void registerTarget(Entity target, object args = null)
        {
            target.registerListener(this);
        }

        public void removeSelf()
        {
            player.removeListener(this);
        }

        public void update(float deltaTime, Entity target)
        {
            foreach (var board in boardLookup.Values)
            {
                board.Tick(deltaTime);
            }
        }
    }
}
