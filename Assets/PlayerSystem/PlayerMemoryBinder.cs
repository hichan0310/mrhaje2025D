// Assets/PlayerSystem/PlayerMemoryBinder.cs
using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;
// using PlayerSystem.Triggers; // (없어도 동작합니다. PowerPolicies가 필요없다면 제외)
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem
{
    /// <summary>
    /// 이벤트 기반 라우팅 버전의 메모리 바인더.
    /// - 각 보드의 EventKey와 들어온 EventArgs의 런타임 타입이 일치할 때만 해당 보드를 실행
    /// - 인벤토리/배치/제거/조회 및 UI 이벤트 유지
    /// </summary>
    public class PlayerMemoryBinder : MonoBehaviour, IEntityEventListener
    {
        [Serializable]
        private class StartingInventoryEntry
        {
            [SerializeField] internal MemoryPieceAsset piece = null;
            [SerializeField, Range(0.1f, 10f)] internal float powerMultiplier = 1f;
        }

        [Serializable]
        private class BoardEntry
        {
            [SerializeField] internal string id = "Board";
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

        [Header("Refs")]
        [SerializeField] private Player player = null;

        [Header("Boards (one per event)")]
        [SerializeField] private List<BoardEntry> boards = new();

        [Header("Global Power Scale")]
        [SerializeField] private float globalPowerScale = 1f;

        [Header("Starting Inventory")]
        [SerializeField] private List<StartingInventoryEntry> startingInventory = new();

        // ===== 오버레이/외부에서 쓰는 공개 상태 =====
        public IReadOnlyList<MemoryPieceInventoryItem> Inventory => inventoryPieces;
        public int ActiveBoardIndex => activeBoardIndex;
        public MemoryBoard ActiveBoard => (activeBoardIndex >= 0 && activeBoardIndex < boardList.Count)
            ? boardList[activeBoardIndex]
            : null;
        public int BoardCount => boardList.Count;

        // ===== UI용 이벤트 =====
        public event Action InventoryChanged;
        public event Action BoardListChanged;
        public event Action<int> ActiveBoardChanged;
        public event Action<int> BoardChanged; // index of changed board

        // ===== 내부 상태 =====
        private readonly List<MemoryPieceInventoryItem> inventoryPieces = new();
        private readonly List<MemoryBoard> boardList = new();
        private readonly Dictionary<MemoryPieceAsset, int> pieceOwnership = new(); // asset -> board index
        private readonly List<MemoryBoard.MemoryPiecePlacementInfo> placementBuffer = new();

        private int activeBoardIndex = -1;

        // 1 프레임용 컨텍스트 (Projectile 보정 등에 사용)
        private MemoryTriggerContext _currentContext;

        private void Awake()
        {
            if (!player) player = GetComponent<Player>();

            BuildBoards();
            InitializeInventory();

            // 플레이어 이벤트 리스너 등록
            registerTarget(player);
        }

        private void OnDestroy()
        {
            boardList.Clear();
            pieceOwnership.Clear();
        }

        // ====================== Public board helpers ======================
        public bool TryGetBoard(int index, out MemoryBoard board)
        {
            if (index >= 0 && index < boardList.Count)
            {
                board = boardList[index];
                return true;
            }
            board = null;
            return false;
        }

        public bool SetActiveBoard(int index)
        {
            if (index < 0 || index >= boardList.Count) return false;
            if (activeBoardIndex == index) return true;
            activeBoardIndex = index;
            ActiveBoardChanged?.Invoke(activeBoardIndex);
            return true;
        }

        // ====================== Inventory ======================
        public bool TryPlaceInventoryPiece(MemoryPieceInventoryItem item, Vector2Int origin, bool locked = false)
        {
            var board = ActiveBoard;
            if (board == null) return false;
            if (!item.Asset) return false;
            if (pieceOwnership.ContainsKey(item.Asset)) return false;

            int idx = FindInventoryIndex(item.Asset, item.PowerMultiplier, true);
            if (idx < 0) return false;

            var entry = inventoryPieces[idx];
            if (!board.TryAddPiece(entry.Asset, origin, entry.PowerMultiplier, locked)) return false;

            pieceOwnership[entry.Asset] = activeBoardIndex;
            inventoryPieces.RemoveAt(idx);
            InventoryChanged?.Invoke();
            BoardChanged?.Invoke(activeBoardIndex);
            return true;
        }

        public bool TryPlaceInventoryPiece(int boardIndex, MemoryPieceInventoryItem item, Vector2Int origin, bool locked = false)
        {
            if (!TryGetBoard(boardIndex, out var board)) return false;
            if (!item.Asset) return false;
            if (pieceOwnership.ContainsKey(item.Asset)) return false;

            int idx = FindInventoryIndex(item.Asset, item.PowerMultiplier, true);
            if (idx < 0) return false;

            var entry = inventoryPieces[idx];
            if (!board.TryAddPiece(entry.Asset, origin, entry.PowerMultiplier, locked)) return false;

            pieceOwnership[entry.Asset] = boardIndex;
            inventoryPieces.RemoveAt(idx);
            InventoryChanged?.Invoke();
            BoardChanged?.Invoke(boardIndex);
            return true;
        }

        public bool RemovePiece(MemoryPieceAsset asset)
        {
            if (!asset) return false;
            if (!pieceOwnership.TryGetValue(asset, out var bIndex)) return false;
            if (!TryGetBoard(bIndex, out var board)) return false;

            if (!board.TryGetPlacement(asset, out var placement)) return false;
            if (!board.RemovePiece(asset)) return false;

            inventoryPieces.Add(new MemoryPieceInventoryItem(placement.Asset, placement.PowerMultiplier));
            pieceOwnership.Remove(asset);
            InventoryChanged?.Invoke();
            BoardChanged?.Invoke(bIndex);
            return true;
        }

        public bool TryAddPieceToInventory(MemoryPieceAsset asset, float multiplier = 1f)
        {
            if (!asset) return false;
            inventoryPieces.Add(new MemoryPieceInventoryItem(asset, multiplier));
            InventoryChanged?.Invoke();
            return true;
        }

        public bool HasInventoryPiece(MemoryPieceInventoryItem item)
            => FindInventoryIndex(item.Asset, item.PowerMultiplier, true) >= 0;

        public bool ContainsPiece(MemoryPieceAsset asset)
            => asset && pieceOwnership.ContainsKey(asset);

        // ====================== Build / Init ======================
        private void BuildBoards()
        {
            boardList.Clear();
            pieceOwnership.Clear();

            for (int i = 0; i < boards.Count; i++)
            {
                var entry = boards[i];
                if (entry == null || entry.board == null) continue;

                // 파워 정책 주입: 없으면 null → 보드 내부에서 기본값 1f 사용
                // entry.board.Initialize(this, PlayerSystem.Triggers.PowerPolicies.Select);
                entry.board.Initialize(this, _ => 1f);

                boardList.Add(entry.board);
            }

            // 기존 배치 소유권 복원
            placementBuffer.Clear();
            for (int bi = 0; bi < boardList.Count; bi++)
            {
                var b = boardList[bi];
                b.GetPiecePlacements(placementBuffer);
                foreach (var placement in placementBuffer)
                    if (placement.Asset) pieceOwnership[placement.Asset] = bi;
            }

            activeBoardIndex = boardList.Count > 0 ? 0 : -1;
            BoardListChanged?.Invoke();
            if (activeBoardIndex >= 0) ActiveBoardChanged?.Invoke(activeBoardIndex);
        }

        private void InitializeInventory()
        {
            inventoryPieces.Clear();
            foreach (var s in startingInventory)
                if (s != null && s.piece)
                    inventoryPieces.Add(new MemoryPieceInventoryItem(s.piece, s.powerMultiplier));

            if (inventoryPieces.Count > 0) InventoryChanged?.Invoke();
        }

        private int FindInventoryIndex(MemoryPieceAsset asset, float multiplier, bool strictMultiplier)
        {
            if (!asset) return -1;
            const float epsilon = 0.001f;
            for (int i = 0; i < inventoryPieces.Count; i++)
            {
                var entry = inventoryPieces[i];
                if (entry.Asset != asset) continue;
                if (!strictMultiplier || Mathf.Abs(entry.PowerMultiplier - multiplier) < epsilon)
                    return i;
            }
            return -1;
        }

        // ====================== IEntityEventListener ======================
        public void eventActive(EventArgs e)
        {
            if (e == null || boardList.Count == 0) return;

            // 1) 1프레임 컨텍스트 생성 (보드 이펙트가 Projectile 보정 등에 사용)
            _currentContext = new MemoryTriggerContext(this, ActiveBoard, Mathf.Max(0f, 1f * globalPowerScale));

            // 2) 보드 라우팅: 각 보드의 EventKey와 e의 타입명이 일치할 때만 실행
            for (int i = 0; i < boardList.Count; i++)
            {
                var b = boardList[i];
                if (b == null) continue;
                if (!b.Matches(e)) continue;          // ← 핵심: eventKey 매칭
                b.recieveEvent(e);
                BoardChanged?.Invoke(i);              // UI 갱신
            }

            // 3) 컨텍스트 회수
            _currentContext?.Complete();
            _currentContext = null;
        }

        public void registerTarget(Entity target, object args = null)
        {
            if (target) target.registerListener(this);
        }

        public void removeSelf()
        {
            if (player) player.removeListener(this);
        }

        public void update(float deltaTime, Entity target)
        {
            for (int i = 0; i < boardList.Count; i++)
                boardList[i].Tick(deltaTime);
        }

        // ====================== 구 API 호환 션트 ======================
        /// <summary>컨텍스트 조회(Projectile 보정 등).</summary>
        public bool TryGetContext(out MemoryTriggerContext ctx)
        {
            ctx = _currentContext;
            return ctx != null;
        }

        /// <summary>
        /// 구 코드 호환용 Trigger(삭제 예정).
        /// ActionTriggerType 대신 dummy object를 받으며, 이벤트 파이프라인으로 변환합니다.
        /// </summary>
        [Obsolete("Use eventActive(EventArgs) with a proper EventArgs instance instead.")]
        public void Trigger(object removedEnum, float basePower = 1f)
        {
            if (!player) return;
            var e = new BinderPowerEvent(player, Mathf.Max(0f, basePower * globalPowerScale));
            eventActive(e);
        }

        private sealed class BinderPowerEvent : EventArgs, IEntityInfo, IPercentInfo
        {
            public Entity entity { get; }
            public float percent { get; }
            public BinderPowerEvent(Entity target, float power)
            {
                name = "BinderPowerEvent";
                entity = target;
                percent = power;
            }
            public override void trigger() { /* no-op */ }
        }
    }
}
