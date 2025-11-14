using System;
using System.Collections.Generic;
using System.Linq;
using EntitySystem;
using PlayerSystem.Effects;
using PlayerSystem.Tiling;
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
            [SerializeField] [Range(0, 3)] internal int rotationSteps = 0;
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
            public IReadOnlyList<Cell> LocalCells { get; }
            public HashSet<Vector2Int> OccupiedCells { get; }
            public float CooldownRemaining { get; private set; }
            public int RotationSteps { get; }

            public MemoryPieceRuntime(MemoryPieceAsset asset, Vector2Int origin, float multiplier, bool locked,
                int rotationSteps)
            {
                Asset = asset;
                Origin = origin;
                Locked = locked;
                PowerMultiplier = multiplier;
                RotationSteps = MemoryPieceTilingUtility.NormalizeRotationSteps(rotationSteps);
                LocalCells = asset ? asset.GetTilingCells(RotationSteps) : Array.Empty<Cell>();
                OccupiedCells = new HashSet<Vector2Int>();
                MemoryPieceTilingUtility.CopyWorldCells(LocalCells, origin, OccupiedCells);

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
            public Vector2Int Origin { get; }
            public IReadOnlyList<Cell> LocalCells { get; }

            public MemoryReinforcementRuntime(MemoryReinforcementZoneAsset asset, Vector2Int origin)
            {
                Asset = asset;
                Origin = origin;
                LocalCells = asset ? asset.GetTilingCells() : Array.Empty<Cell>();
                OccupiedCells = new HashSet<Vector2Int>();
                MemoryPieceTilingUtility.CopyWorldCells(LocalCells, origin, OccupiedCells);
            }
        }

        public readonly struct MemoryPiecePlacementInfo
        {
            public MemoryPieceAsset Asset { get; }
            public Vector2Int Origin { get; }
            public float PowerMultiplier { get; }
            public bool Locked { get; }
            public int RotationSteps { get; }
            public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;

            private readonly Vector2Int[] occupiedCells;

            internal MemoryPiecePlacementInfo(
                MemoryPieceAsset asset,
                Vector2Int origin,
                float powerMultiplier,
                bool locked,
                int rotationSteps,
                IEnumerable<Vector2Int> occupied)
            {
                if (asset == null)
                {
                    throw new ArgumentNullException(nameof(asset));
                }

                if (occupied == null)
                {
                    throw new ArgumentNullException(nameof(occupied));
                }

                Asset = asset;
                Origin = origin;
                PowerMultiplier = powerMultiplier;
                Locked = locked;
                RotationSteps = MemoryPieceTilingUtility.NormalizeRotationSteps(rotationSteps);
                occupiedCells = occupied.ToArray();
            }
        }

        public readonly struct MemoryReinforcementInfo
        {
            public MemoryReinforcementZoneAsset Zone { get; }
            public Vector2Int Origin { get; }
            public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;

            private readonly Vector2Int[] occupiedCells;

            internal MemoryReinforcementInfo(
                MemoryReinforcementZoneAsset zone,
                Vector2Int origin,
                IEnumerable<Vector2Int> occupied)
            {
                if (zone == null)
                {
                    throw new ArgumentNullException(nameof(zone));
                }

                if (occupied == null)
                {
                    throw new ArgumentNullException(nameof(occupied));
                }

                Zone = zone;
                Origin = origin;
                occupiedCells = occupied.ToArray();
            }
        }

        [Header("Board Settings")] [SerializeField]
        private Vector2Int gridSize = new Vector2Int(6, 6);

        [SerializeField] private List<MemoryResourcePool> resources = new();
        [SerializeField] private List<MemoryPiecePlacement> startingPieces = new();
        [SerializeField] private List<MemoryReinforcementPlacement> reinforcementZones = new();

        private readonly List<MemoryPieceRuntime> runtimePieces = new();
        private readonly List<MemoryReinforcementRuntime> runtimeReinforcements = new();
        private readonly Dictionary<MemoryPieceAsset, MemoryPieceRuntime> runtimeLookup = new();
        private readonly List<MemoryPieceRuntime> statBuffPieces = new();
        private readonly List<MemoryPieceRuntime> nonBuffPieces = new();
        private readonly List<Vector2Int> placementCellBuffer = new();
        private ActionTriggerType boardTrigger = ActionTriggerType.None;

        public event Action<MemoryPieceAsset, float>? OnPieceTriggered;
        public event Action<MemoryPieceAsset>? OnPieceAdded;
        public event Action<MemoryPieceAsset>? OnPieceRemoved;

        public Vector2Int GridSize => gridSize;
        public IReadOnlyList<MemoryResourcePool> Resources => resources;

        public void GetPiecePlacements(List<MemoryPiecePlacementInfo> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            foreach (var runtime in runtimePieces)
            {
                buffer.Add(new MemoryPiecePlacementInfo(runtime.Asset, runtime.Origin, runtime.PowerMultiplier,
                    runtime.Locked, runtime.RotationSteps, runtime.OccupiedCells));
            }
        }

        public bool TryGetPlacement(MemoryPieceAsset asset, out MemoryPiecePlacementInfo info)
        {
            if (asset && runtimeLookup.TryGetValue(asset, out var runtime))
            {
                info = new MemoryPiecePlacementInfo(runtime.Asset, runtime.Origin, runtime.PowerMultiplier,
                    runtime.Locked, runtime.RotationSteps, runtime.OccupiedCells);
                return true;
            }

            info = default;
            return false;
        }

        public void GetReinforcementPlacements(List<MemoryReinforcementInfo> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            foreach (var runtime in runtimeReinforcements)
            {
                buffer.Add(new MemoryReinforcementInfo(runtime.Asset, runtime.Origin, runtime.OccupiedCells));
            }
        }

        public void Initialize(PlayerMemoryBinder binder, ActionTriggerType trigger)
        {
            if (binder == null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            runtimePieces.Clear();
            runtimeReinforcements.Clear();
            runtimeLookup.Clear();
            boardTrigger = trigger;

            foreach (var resource in resources)
            {
                resource.Initialize();
            }

            foreach (var placement in startingPieces)
            {
                if (!placement.piece) continue;
                TryAddPieceInternal(placement.piece, placement.origin, placement.powerMultiplier, placement.locked,
                    placement.rotationSteps, true);
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

        public void Trigger(ActionTriggerType triggerType, Entity entity, float basePower,
            MemoryTriggerContext? context)
        {
            if (!MatchesTrigger(triggerType) || runtimePieces.Count == 0)
            {
                return;
            }

            statBuffPieces.Clear();
            nonBuffPieces.Clear();

            foreach (var piece in runtimePieces)
            {
                if (piece?.Asset?.Effect is ApplyStatBuffEffectAsset)
                {
                    statBuffPieces.Add(piece);
                }
                else
                {
                    nonBuffPieces.Add(piece);
                }
            }

            foreach (var piece in statBuffPieces)
            {
                TriggerRuntimePiece(piece, entity, basePower, context);
            }

            foreach (var piece in nonBuffPieces)
            {
                TriggerRuntimePiece(piece, entity, basePower, context);
            }
        }

        public bool CanPlacePiece(MemoryPieceAsset asset, Vector2Int origin, int rotationSteps = 0)
        {
            return IsPlacementValid(asset, origin, rotationSteps);
        }

        public bool TryAddPiece(MemoryPieceAsset asset, Vector2Int origin, float multiplier = 1f, bool locked = false,
            int rotationSteps = 0)
        {
            return TryAddPieceInternal(asset, origin, multiplier, locked, rotationSteps, false);
        }

        private bool TryAddPieceInternal(MemoryPieceAsset asset, Vector2Int origin, float multiplier, bool locked,
            int rotationSteps, bool initializing)
        {
            if (!asset)
            {
                return false;
            }

            rotationSteps = MemoryPieceTilingUtility.NormalizeRotationSteps(rotationSteps);

            if (!IsTriggerCompatible(asset))
            {
                return false;
            }

            if (runtimeLookup.ContainsKey(asset))
            {
                return false;
            }

            if (!IsPlacementValid(asset, origin, rotationSteps))
            {
                return false;
            }

            var runtime = new MemoryPieceRuntime(asset, origin, multiplier, locked, rotationSteps);
            runtimePieces.Add(runtime);
            runtimeLookup[asset] = runtime;

            if (!initializing)
            {
                startingPieces.Add(new MemoryPiecePlacement
                {
                    piece = asset,
                    origin = origin,
                    powerMultiplier = multiplier,
                    locked = locked,
                    rotationSteps = rotationSteps,
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

        private bool MatchesTrigger(ActionTriggerType triggerType)
        {
            if (triggerType == ActionTriggerType.None)
            {
                return false;
            }

            if (boardTrigger == ActionTriggerType.None)
            {
                return true;
            }

            return triggerType.HasFlag(boardTrigger);
        }

        private bool IsTriggerCompatible(MemoryPieceAsset asset)
        {
            if (!asset)
            {
                return false;
            }

            if (boardTrigger == ActionTriggerType.None)
            {
                return true;
            }

            return asset.IsTriggerAllowed(boardTrigger);
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

        private void TriggerRuntimePiece(MemoryPieceRuntime runtime, Entity entity, float basePower,
            MemoryTriggerContext? context)
        {
            if (runtime?.Asset == null)
            {
                return;
            }

            if (!CanActivate(runtime))
            {
                return;
            }

            float power = basePower * runtime.Asset.BasePower * runtime.PowerMultiplier *
                          CalculateReinforcementMultiplier(runtime);
            if (!TryConsumeResource(runtime.Asset))
            {
                return;
            }

            context?.SetCurrentPiece(runtime.Asset, power);
            runtime.Asset.Effect?.trigger(entity, power);
            runtime.SetCooldown();
            OnPieceTriggered?.Invoke(runtime.Asset, power);
        }

        private bool IsPlacementValid(MemoryPieceAsset asset, Vector2Int origin, int rotationSteps)
        {
            var localCells = asset?.GetTilingCells(rotationSteps);
            if (localCells == null || localCells.Count == 0)
            {
                return false;
            }

            if (!MemoryPieceTilingUtility.FitsInsideBoard(localCells, origin, gridSize))
            {
                return false;
            }

            MemoryPieceTilingUtility.CopyWorldCells(localCells, origin, placementCellBuffer);
            foreach (var cell in placementCellBuffer)
            {
                if (runtimePieces.Any(other => other.OccupiedCells.Contains(cell)))
                {
                    return false;
                }
            }

            return true;
        }

        public void recieveEvent(EntitySystem.Events.EventArgs eventArgs)
        {
            
        }
    }
}