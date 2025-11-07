// Assets/PlayerSystem/MemoryBoard.cs
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem
{
    /// <summary>
    /// 이벤트 기반 메모리 보드:
    /// - 트리거/보드 타입 호환성 제약 제거 (모든 피스 배치 허용)
    /// - 자원 소모/쿨다운/강화 보너스 유지
    /// - UI/Binder가 사용하는 내부 타입/조회 API 유지
    /// - 이벤트를 받아 power 정책으로 계산 후 피스 발동
    /// </summary>
    [Serializable]
    public class MemoryBoard
    {
        private PlayerMemoryBinder ownerBinder;
        [Header("Event Routing")]
        [SerializeField] private string eventKey = ""; // 예: "BasicAttackExecuteEvent"
        public string EventKey => eventKey;

        // ====== 직렬화 저장용 배치 데이터 ======
        [Serializable]
        private class MemoryPiecePlacement
        {
            [SerializeField] internal MemoryPieceAsset piece = null;
            [SerializeField] internal Vector2Int origin = Vector2Int.zero;
            [SerializeField, Range(0.1f, 10f)] internal float powerMultiplier = 1f;
            [SerializeField] internal bool locked = false;
        }

        [Serializable]
        private class MemoryReinforcementPlacement
        {
            [SerializeField] internal MemoryReinforcementZoneAsset zone = null;
            [SerializeField] internal Vector2Int origin = Vector2Int.zero;
        }
        internal bool Matches(EventArgs e)
        {
            if (string.IsNullOrEmpty(eventKey) || e == null) return false;
            var key = e.GetType().Name;
            return string.Equals(eventKey, key, StringComparison.Ordinal);
        }
        // ====== 런타임 관리용 ======
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
                    OccupiedCells.Add(origin + offset);

                CooldownRemaining = 0f;
            }

            public void Tick(float deltaTime)
            {
                if (CooldownRemaining > 0f)
                    CooldownRemaining = Mathf.Max(0f, CooldownRemaining - deltaTime);
            }

            public void SetCooldown() => CooldownRemaining = Asset.CooldownSeconds;
        }

        private class MemoryReinforcementRuntime
        {
            public MemoryReinforcementZoneAsset Asset { get; }
            public HashSet<Vector2Int> OccupiedCells { get; }
            public Vector2Int Origin { get; }

            public MemoryReinforcementRuntime(MemoryReinforcementZoneAsset asset, Vector2Int origin)
            {
                Asset = asset;
                Origin = origin;
                OccupiedCells = new HashSet<Vector2Int>();
                foreach (var offset in asset.ShapeCells)
                    OccupiedCells.Add(origin + offset);
            }
        }

        // ====== 외부 조회용 뷰 모델 ======
        public readonly struct MemoryPiecePlacementInfo
        {
            public MemoryPieceAsset Asset { get; }
            public Vector2Int Origin { get; }
            public float PowerMultiplier { get; }
            public bool Locked { get; }
            public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;

            private readonly Vector2Int[] occupiedCells;


            internal MemoryPiecePlacementInfo(
                MemoryPieceAsset asset,
                Vector2Int origin,
                float powerMultiplier,
                bool locked,
                IEnumerable<Vector2Int> occupied)
            {
                if (asset == null) throw new ArgumentNullException(nameof(asset));
                if (occupied == null) throw new ArgumentNullException(nameof(occupied));

                Asset = asset;
                Origin = origin;
                PowerMultiplier = powerMultiplier;
                Locked = locked;
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
                if (zone == null) throw new ArgumentNullException(nameof(zone));
                if (occupied == null) throw new ArgumentNullException(nameof(occupied));

                Zone = zone;
                Origin = origin;
                occupiedCells = occupied.ToArray();
            }
        }

        // ====== 보드 설정 / 직렬화 ======
        [Header("Board Settings")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(6, 6);

        [SerializeField] private List<MemoryResourcePool> resources = new();
        [SerializeField] private List<MemoryPiecePlacement> startingPieces = new();
        [SerializeField] private List<MemoryReinforcementPlacement> reinforcementZones = new();

        // ====== 런타임 컨테이너 ======
        private readonly List<MemoryPieceRuntime> runtimePieces = new();
        private readonly List<MemoryReinforcementRuntime> runtimeReinforcements = new();
        private readonly Dictionary<MemoryPieceAsset, MemoryPieceRuntime> runtimeLookup = new();
        private readonly List<MemoryPieceRuntime> statBuffPieces = new();
        private readonly List<MemoryPieceRuntime> nonBuffPieces = new();

        // 이벤트  power 계산 정책 (주입)
        private Func<EventArgs, float?> powerSelector;

        // ====== 이벤트 ======
        public event Action<MemoryPieceAsset, float>? OnPieceTriggered;
        public event Action<MemoryPieceAsset>? OnPieceAdded;
        public event Action<MemoryPieceAsset>? OnPieceRemoved;

        // ====== 프로퍼티 ======
        public Vector2Int GridSize => gridSize;
        public IReadOnlyList<MemoryResourcePool> Resources => resources;

        // ====== 외부 조회 API (UI/Binder가 사용) ======
        public void GetPiecePlacements(List<MemoryPiecePlacementInfo> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            buffer.Clear();
            foreach (var r in runtimePieces)
                buffer.Add(new MemoryPiecePlacementInfo(r.Asset, r.Origin, r.PowerMultiplier, r.Locked, r.OccupiedCells));
        }

        public bool TryGetPlacement(MemoryPieceAsset asset, out MemoryPiecePlacementInfo info)
        {
            if (asset && runtimeLookup.TryGetValue(asset, out var r))
            {
                info = new MemoryPiecePlacementInfo(r.Asset, r.Origin, r.PowerMultiplier, r.Locked, r.OccupiedCells);
                return true;
            }
            info = default;
            return false;
        }

        public void GetReinforcementPlacements(List<MemoryReinforcementInfo> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            buffer.Clear();
            foreach (var r in runtimeReinforcements)
                buffer.Add(new MemoryReinforcementInfo(r.Asset, r.Origin, r.OccupiedCells));
        }

        // ====== 초기화 / 틱 ======
        public void Initialize(PlayerMemoryBinder binder, Func<EventArgs, float?> powerSelector)
        {
            if (binder == null) throw new ArgumentNullException(nameof(binder));
            this.ownerBinder = binder;
            this.powerSelector = powerSelector ?? (_ => 1f);

            runtimePieces.Clear();
            runtimeReinforcements.Clear();
            runtimeLookup.Clear();

            foreach (var resource in resources)
                resource.Initialize();

            foreach (var placement in startingPieces)
            {
                if (!placement.piece) continue;
                TryAddPieceInternal(placement.piece, placement.origin, placement.powerMultiplier, placement.locked, initializing: true);
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
                resource.Tick(deltaTime);

            foreach (var piece in runtimePieces)
                piece.Tick(deltaTime);
        }

        // ====== 이벤트 수신 (ActionTriggerType 제거) ======

        public void recieveEvent(EventArgs eventArgs)
        {
            Debug.Log($"[Board:{EventKey}] Recv {eventArgs.GetType().Name}");
            // 보드-이벤트 키 매칭 확인 (안 맞으면 바로 종료)
            if (!Matches(eventArgs)) return;

            if (eventArgs == null || runtimePieces.Count == 0) return;

            var p = powerSelector?.Invoke(eventArgs);
            if (p is not float basePower || basePower <= 0f) return;

            Entity entity = (eventArgs as IEntityInfo)?.entity;
            if (entity == null) return;

            MemoryTriggerContext ctx = null;
            ownerBinder?.TryGetContext(out ctx);

            statBuffPieces.Clear();
            nonBuffPieces.Clear();

            foreach (var piece in runtimePieces)
            {
                if (piece?.Asset?.Effect is ApplyStatBuffEffectAsset) statBuffPieces.Add(piece);
                else nonBuffPieces.Add(piece);
            }

            foreach (var piece in statBuffPieces)
                TriggerRuntimePiece(piece, entity, basePower, ctx);

            foreach (var piece in nonBuffPieces)
                TriggerRuntimePiece(piece, entity, basePower, ctx);
        }

        // ====== 배치/제거 ======
        public bool TryAddPiece(MemoryPieceAsset asset, Vector2Int origin, float multiplier = 1f, bool locked = false)
            => TryAddPieceInternal(asset, origin, multiplier, locked, initializing: false);

        private bool TryAddPieceInternal(MemoryPieceAsset asset, Vector2Int origin, float multiplier, bool locked, bool initializing)
        {
            if (!asset) return false;

            // 제한 해제: 트리거/보드 타입 호환성 검사 제거
            if (runtimeLookup.ContainsKey(asset)) return false;
            if (!IsPlacementValid(asset, origin)) return false;

            var runtime = new MemoryPieceRuntime(asset, origin, multiplier, locked);
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
                });
                OnPieceAdded?.Invoke(asset);
            }
            return true;
        }

        public bool RemovePiece(MemoryPieceAsset asset)
        {
            if (!runtimeLookup.TryGetValue(asset, out var runtime)) return false;

            runtimePieces.Remove(runtime);
            runtimeLookup.Remove(asset);

            var placement = startingPieces.FirstOrDefault(p => p.piece == asset);
            if (placement != null) startingPieces.Remove(placement);

            OnPieceRemoved?.Invoke(asset);
            return true;
        }

        public bool Contains(MemoryPieceAsset asset) => runtimeLookup.ContainsKey(asset);

        // ====== 자원 관리 ======
        public void AddResource(MemoryResourceType type, float amount)
        {
            if (type == MemoryResourceType.None || amount <= 0f) return;

            var pool = resources.FirstOrDefault(r => r.ResourceType == type);
            if (pool == null)
            {
                pool = new MemoryResourcePool();
                pool.ForceSetup(type, Mathf.Max(amount, 10f), 0f);
                resources.Add(pool);
            }
            pool.Add(amount);
        }

        private bool TryConsumeResource(MemoryPieceAsset asset)
        {
            if (asset.IsCore) return true;
            if (asset.ResourceType == MemoryResourceType.None || asset.ResourceCost <= 0f) return true;

            var pool = resources.FirstOrDefault(r => r.ResourceType == asset.ResourceType);
            if (pool == null) return false;
            return pool.TryConsume(asset.ResourceCost);
        }

        // ====== 발동 경로 (쿨다운/강화/자원 유지) ======
        private bool CanActivate(MemoryPieceRuntime runtime)
        {
            if (runtime.Asset == null) return false;
            if (runtime.Asset.Effect == null) return false;
            if (runtime.Asset.CooldownSeconds > 0f && runtime.CooldownRemaining > 0f) return false;
            // 코어 메모리는 항상 가능
            return true;
        }

        private float CalculateReinforcementMultiplier(MemoryPieceRuntime runtime)
        {
            float bonusPercent = 0f;
            foreach (var reinforcement in runtimeReinforcements)
            {
                if (runtime.OccupiedCells.Overlaps(reinforcement.OccupiedCells))
                    bonusPercent += reinforcement.Asset.BonusPercent;
            }
            return 1f + bonusPercent / 100f;
        }

        private void TriggerRuntimePiece(MemoryPieceRuntime runtime, Entity entity, float basePower, MemoryTriggerContext? context)
        {
            if (runtime?.Asset == null) return;
            if (!CanActivate(runtime)) return;

            float power = basePower * runtime.Asset.BasePower * runtime.PowerMultiplier * CalculateReinforcementMultiplier(runtime);
            if (!TryConsumeResource(runtime.Asset)) return;

            context?.SetCurrentPiece(runtime.Asset, power);
            runtime.Asset.Effect?.trigger(entity, power);
            runtime.SetCooldown();
            OnPieceTriggered?.Invoke(runtime.Asset, power);
        }

        // ====== 배치 유효성 / 경계 ======
        private bool IsPlacementValid(MemoryPieceAsset asset, Vector2Int origin)
        {
            foreach (var offset in asset.ShapeCells)
            {
                Vector2Int cell = origin + offset;
                if (!IsInsideBoard(cell)) return false;

                if (runtimePieces.Any(other => other.OccupiedCells.Contains(cell))) return false;
            }
            return true;
        }

        private bool IsInsideBoard(Vector2Int cell)
            => cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;
    }
}
