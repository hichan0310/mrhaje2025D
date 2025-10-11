using System;
using System.Collections.Generic;
using EntitySystem;
using GameBackend;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Component that wires the serialized memory board to the runtime player instance.
    /// </summary>
    public class PlayerMemoryBinder : MonoBehaviour
    {
        [Serializable]
        private class StartingInventoryEntry
        {
            [SerializeField] internal MemoryPieceAsset piece = null;
            [SerializeField] [Range(0.1f, 10f)] internal float powerMultiplier = 1f;
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
        [SerializeField] private MemoryBoard board = new();
        [SerializeField] private float globalPowerScale = 1f;
        [SerializeField] private List<StartingInventoryEntry> startingInventory = new();

        public MemoryBoard Board => board;
        public IReadOnlyList<MemoryPieceInventoryItem> Inventory => inventoryPieces;

        public event Action? InventoryChanged;

        private readonly List<MemoryPieceInventoryItem> inventoryPieces = new();

        private void Awake()
        {
            if (!player)
            {
                player = GetComponent<Player>();
            }

            board.Initialize(this);

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

        private void Update()
        {
            board.Tick(TimeManager.deltaTime);
        }

        public void Trigger(ActionTriggerType triggerType, float basePower = 1f)
        {
            if (!player)
            {
                return;
            }

            float power = Mathf.Max(0f, basePower * globalPowerScale);
            board.Trigger(triggerType, player, power);
        }

        public bool TryAddPiece(MemoryPieceAsset asset, Vector2Int origin, float multiplier = 1f, bool locked = false)
        {
            if (!asset)
            {
                return false;
            }

            int index = FindInventoryIndex(asset, multiplier, true);
            if (index < 0)
            {
                index = FindInventoryIndex(asset, multiplier, false);
            }

            if (index < 0)
            {
                return false;
            }

            var entry = inventoryPieces[index];
            if (!board.TryAddPiece(entry.Asset, origin, entry.PowerMultiplier, locked))
            {
                return false;
            }

            inventoryPieces.RemoveAt(index);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool RemovePiece(MemoryPieceAsset asset)
        {
            if (!asset)
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

        public bool TryPlaceInventoryPiece(MemoryPieceInventoryItem item, Vector2Int origin, bool locked = false)
        {
            int index = FindInventoryIndex(item.Asset, item.PowerMultiplier, true);
            if (index < 0)
            {
                return false;
            }

            var entry = inventoryPieces[index];
            if (!board.TryAddPiece(entry.Asset, origin, entry.PowerMultiplier, locked))
            {
                return false;
            }

            inventoryPieces.RemoveAt(index);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool HasInventoryPiece(MemoryPieceInventoryItem item)
        {
            return FindInventoryIndex(item.Asset, item.PowerMultiplier, true) >= 0;
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
    }
}
