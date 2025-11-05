using System;
using System.Collections.Generic;
using System.Linq;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// 플레이어의 메모리 트리거와 인벤토리를 관리하는 핵심 클래스.
    /// 더 이상 공간적 보드(좌표, 셀)를 사용하지 않고,
    /// 단순히 조각들의 발동/등록만 담당한다.
    /// </summary>
    public class PlayerMemoryBinder : MonoBehaviour, IEntityEventListener
    {
        [Serializable]
        public struct InventoryItem
        {
            public MemoryPieceAsset Asset;
            public float PowerMultiplier;

            public InventoryItem(MemoryPieceAsset a, float mul)
            {
                Asset = a;
                PowerMultiplier = mul <= 0f ? 1f : mul;
            }
        }

        [Header("Inventory")]
        [SerializeField] private List<InventoryItem> inventoryPieces = new();

        // 자원 풀 (Energy, Focus 등)
        private readonly Dictionary<MemoryResourceType, MemoryResourcePool> _resourcePools = new();

        public event Action InventoryChanged;

        private void Awake()
        {
            // 자원 풀 초기화
            foreach (MemoryResourceType type in Enum.GetValues(typeof(MemoryResourceType)))
            {
                if (type == MemoryResourceType.None) continue;
                _resourcePools[type] = new MemoryResourcePool();
                _resourcePools[type].ForceSetup(type, 100f, 0f);
            }
        }

        // ===== 인벤토리 =====
        public IReadOnlyList<InventoryItem> Inventory => inventoryPieces;

        public bool TryAddPieceToInventory(MemoryPieceAsset asset, float multiplier)
        {
            if (!asset) return false;
            inventoryPieces.Add(new InventoryItem(asset, multiplier <= 0f ? 1f : multiplier));
            InventoryChanged?.Invoke();
            return true;
        }

        public bool RemovePiece(MemoryPieceAsset asset)
        {
            int idx = inventoryPieces.FindIndex(i => i.Asset == asset);
            if (idx < 0) return false;
            inventoryPieces.RemoveAt(idx);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool HasPiece(MemoryPieceAsset asset)
        {
            return inventoryPieces.Any(i => i.Asset == asset);
        }

        // ===== 트리거 발동 =====
        /// <summary>
        /// 현재 등록된 모든 조각을 발동한다.
        /// </summary>
        public void TriggerAll(Entity target = null)
        {
            var entity = target ?? GetComponent<Entity>();
            if (!entity) return;

            foreach (var item in inventoryPieces)
            {
                try
                {
                    item.Asset.Trigger(entity);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        // ===== IEntityEventListener 구현 =====
        public void eventActive(EntitySystem.Events.EventArgs e) { }
        public void update(float deltaTime, Entity entity)
        {
            foreach (var pool in _resourcePools.Values)
                pool.Tick(deltaTime);
        }
        public void registerTarget(Entity target, object args = null) => target.registerListener(this);
        public void removeSelf() => Destroy(this);

        // ===== 리소스 관리 =====
        public void AddResource(MemoryResourceType type, float amount)
        {
            if (type == MemoryResourceType.None || amount <= 0f) return;
            if (_resourcePools.TryGetValue(type, out var pool))
                pool.Add(amount);
        }

        public bool TryConsumeResource(MemoryResourceType type, float cost)
        {
            if (type == MemoryResourceType.None) return true;
            if (!_resourcePools.TryGetValue(type, out var pool)) return false;
            return pool.TryConsume(cost);
        }
    }
}
