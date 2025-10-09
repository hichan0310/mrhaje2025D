using System;
using System.Collections.Generic;
using System.Linq;
using EntitySystem;
using UnityEngine;

namespace PlayerSystem
{
    [Serializable]
    public class MemoryBoard
    {
        [Serializable]
        private class MemoryPiecePlacement
        {
            [SerializeField] internal MemoryPieceAsset piece = null;
            [SerializeField] internal Vector2Int origin = Vector2Int.zero;
            [SerializeField] [Range(0.1f, 10f)] internal float powerMultiplier = 1f;
            [SerializeField] internal bool locked = false;
        }

        [Serializable]
        private class MemoryReinforcementPlacement
        {
            [SerializeField] internal MemoryReinforcementZoneAsset zone = null;
            [SerializeField] internal Vector2Int origin = Vector2Int.zero;
        }

        private class MemoryPieceRuntime
        {
            public MemoryPieceAsset Asset { get; }
            public Vector2Int Origin { get; }
            public bool Locked { get; }
            public float PowerMultiplier { get; }
            public HashSet<Vector2Int> OccupiedCells { get; }
            public float CooldownRemaining { get; private set; }

            public MemoryPieceRuntime(MemoryPieceAsset asset, Vector2Int origin, float multiplier, bool locked)
            {
                Asset = asset;
                Origin = origin;
                Locked = locked;
                PowerMultiplier = multiplier;
                OccupiedCells = new HashSet<Vector2Int>();
                foreach (var offset in asset.ShapeCells)
                {
                    OccupiedCells.Add(origin + offset);
                }
                CooldownRemaining = 0f;
            }

            public void Tick(float deltaTime)
            {
                if (CooldownRemaining > 0f)
                {
                    CooldownRemaining = Mathf.Max(0f, CooldownRemaining - deltaTime);
                }
            }

            public void SetCooldown()
            {
                CooldownRemaining = Asset.CooldownSeconds;
            }
        }

        private class MemoryReinforcementRuntime
        {
            public MemoryReinforcementZoneAsset Asset { get; }
            public HashSet<Vector2Int> OccupiedCells { get; }

            public MemoryReinforcementRuntime(MemoryReinforcementZoneAsset asset, Vector2Int origin)
            {
                Asset = asset;
                OccupiedCells = new HashSet<Vector2Int>();
                foreach (var offset in asset.ShapeCells)
                {
                    OccupiedCells.Add(origin + offset);
                }
            }
        }

        [Header("Board Settings")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(6, 6);
        [SerializeField] private List<MemoryResourcePool> resources = new();
        [SerializeField] private List<MemoryPiecePlacement> startingPieces = new();
        [SerializeField] private List<MemoryReinforcementPlacement> reinforcementZones = new();

        private readonly List<MemoryPieceRuntime> runtimePieces = new();
        private readonly List<MemoryReinforcementRuntime> runtimeReinforcements = new();
        private readonly Dictionary<ActionTriggerType, List<MemoryPieceRuntime>> piecesByTrigger = new();
        private readonly Dictionary<MemoryPieceAsset, MemoryPieceRuntime> runtimeLookup = new();
        private readonly List<MemoryPieceRuntime> triggerBuffer = new();

        public event Action<MemoryPieceAsset, float>? OnPieceTriggered;
        public event Action<MemoryPieceAsset>? OnPieceAdded;
        public event Action<MemoryPieceAsset>? OnPieceRemoved;

        public Vector2Int GridSize => gridSize;
        public IReadOnlyList<MemoryResourcePool> Resources => resources;

        public void Initialize(PlayerMemoryBinder binder)
        {
            if (binder == null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            runtimePieces.Clear();
            runtimeReinforcements.Clear();
            piecesByTrigger.Clear();
            runtimeLookup.Clear();

            foreach (var resource in resources)
            {
                resource.Initialize();
            }

            foreach (var placement in startingPieces)
            {
                if (!placement.piece) continue;
                TryAddPieceInternal(placement.piece, placement.origin, placement.powerMultiplier, placement.locked, true);
            }

            foreach (var reinforcement in reinforcementZones)
            {
                if (!reinforcement.zone) continue;
                runtimeReinforcements.Add(new MemoryReinforcementRuntime(reinforcement.zone, reinforcement.origin));
            }
        }

        public void Tick(float deltaTime)
        {
            foreach (var resource in resources)
            {
                resource.Tick(deltaTime);
            }

            foreach (var piece in runtimePieces)
            {
                piece.Tick(deltaTime);
            }
        }

        public void Trigger(ActionTriggerType triggerType, Entity entity, float basePower)
        {
            var list = GatherPieces(triggerType);
            if (list.Count == 0)
            {
                return;
            }

            foreach (var piece in list)
            {
                if (!CanActivate(piece))
                {
                    continue;
                }

                float power = basePower * piece.Asset.BasePower * piece.PowerMultiplier * CalculateReinforcementMultiplier(piece);
                if (TryConsumeResource(piece.Asset))
                {
                    piece.Asset.Effect?.trigger(entity, power);
                    piece.SetCooldown();
                    OnPieceTriggered?.Invoke(piece.Asset, power);
                }
            }
        }

        private List<MemoryPieceRuntime> GatherPieces(ActionTriggerType triggerType)
        {
            triggerBuffer.Clear();

            if (triggerType == ActionTriggerType.None)
            {
                return triggerBuffer;
            }

            if (piecesByTrigger.TryGetValue(triggerType, out var direct))
            {
                triggerBuffer.AddRange(direct);
                return triggerBuffer;
            }

            foreach (var pair in piecesByTrigger)
            {
                if (pair.Key == ActionTriggerType.None)
                {
                    continue;
                }

                if (triggerType.HasFlag(pair.Key))
                {
                    triggerBuffer.AddRange(pair.Value);
                }
            }

            return triggerBuffer;
        }

        public bool TryAddPiece(MemoryPieceAsset asset, Vector2Int origin, float multiplier = 1f, bool locked = false)
        {
            return TryAddPieceInternal(asset, origin, multiplier, locked, false);
        }

        private bool TryAddPieceInternal(MemoryPieceAsset asset, Vector2Int origin, float multiplier, bool locked, bool initializing)
        {
            if (!asset)
            {
                return false;
            }

            if (runtimeLookup.ContainsKey(asset))
            {
                return false;
            }

            if (!IsPlacementValid(asset, origin))
            {
                return false;
            }

            var runtime = new MemoryPieceRuntime(asset, origin, multiplier, locked);
            runtimePieces.Add(runtime);
            runtimeLookup[asset] = runtime;

            if (!piecesByTrigger.TryGetValue(asset.TriggerType, out var list))
            {
                list = new List<MemoryPieceRuntime>();
                piecesByTrigger[asset.TriggerType] = list;
            }
            list.Add(runtime);

            if (!initializing)
            {
                startingPieces.Add(new MemoryPiecePlacement
                {
                    piece = asset,
                    origin = origin,
                    powerMultiplier = multiplier,
                    locked = locked,
                });
                OnPieceAdded?.Invoke(asset);
            }

            return true;
        }

        public bool RemovePiece(MemoryPieceAsset asset)
        {
            if (!runtimeLookup.TryGetValue(asset, out var runtime))
            {
                return false;
            }

            runtimePieces.Remove(runtime);
            runtimeLookup.Remove(asset);
            if (piecesByTrigger.TryGetValue(asset.TriggerType, out var list))
            {
                list.Remove(runtime);
            }

            var placement = startingPieces.FirstOrDefault(p => p.piece == asset);
            if (placement != null)
            {
                startingPieces.Remove(placement);
            }

            OnPieceRemoved?.Invoke(asset);
            return true;
        }

        public bool Contains(MemoryPieceAsset asset) => runtimeLookup.ContainsKey(asset);

        public void AddResource(MemoryResourceType type, float amount)
        {
            if (type == MemoryResourceType.None || amount <= 0f)
            {
                return;
            }

            var pool = resources.FirstOrDefault(r => r.ResourceType == type);
            if (pool == null)
            {
                pool = new MemoryResourcePool();
                pool.ForceSetup(type, Mathf.Max(amount, 10f), 0f);
                resources.Add(pool);
            }

            pool.Add(amount);
        }

        private bool CanActivate(MemoryPieceRuntime runtime)
        {
            if (runtime.Asset == null)
            {
                return false;
            }

            if (runtime.Asset.Effect == null)
            {
                return false;
            }

            if (runtime.Asset.CooldownSeconds > 0f && runtime.CooldownRemaining > 0f)
            {
                return false;
            }

            if (runtime.Asset.IsCore)
            {
                // core memories are always available regardless of resources
                return true;
            }

            return true;
        }

        private bool TryConsumeResource(MemoryPieceAsset asset)
        {
            if (asset.IsCore)
            {
                return true;
            }

            if (asset.ResourceType == MemoryResourceType.None || asset.ResourceCost <= 0f)
            {
                return true;
            }

            var pool = resources.FirstOrDefault(r => r.ResourceType == asset.ResourceType);
            if (pool == null)
            {
                return false;
            }

            return pool.TryConsume(asset.ResourceCost);
        }

        private float CalculateReinforcementMultiplier(MemoryPieceRuntime runtime)
        {
            float bonusPercent = 0f;
            foreach (var reinforcement in runtimeReinforcements)
            {
                if (runtime.OccupiedCells.Overlaps(reinforcement.OccupiedCells))
                {
                    bonusPercent += reinforcement.Asset.BonusPercent;
                }
            }

            return 1f + bonusPercent / 100f;
        }

        private bool IsPlacementValid(MemoryPieceAsset asset, Vector2Int origin)
        {
            foreach (var offset in asset.ShapeCells)
            {
                Vector2Int cell = origin + offset;
                if (!IsInsideBoard(cell))
                {
                    return false;
                }

                if (runtimePieces.Any(other => other.OccupiedCells.Contains(cell)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInsideBoard(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;
        }
    }
}
