using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    /// <summary>
    /// 순수 보드 모델: 배치/제거/조회만 담당. 이펙트 실행/타이머/이벤트 로직 없음.
    /// </summary>
    [Serializable]
    public sealed class Board
    {
        [Serializable]
        public struct Placement
        {
            public object polyomino;  // 실제 타입은 Polyomino(또는 그 파생). 모델은 구체 타입에 의존하지 않음.
            public int stateIndex;    // 회전/반전 등 상태 인덱스
            public int x;             // 원점 X
            public int y;             // 원점 Y
        }

        public int Width { get; }
        public int Height { get; }

        private readonly List<Placement> _placements = new();
        public event Action? OnChanged;

        public Board(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
        }

        /// <summary>배치 시도. 겹침/경계 체크만 수행.</summary>
        public bool TryPlace(object polyomino, int stateIndex, int ax, int ay, out Placement placement)
        {
            placement = default;
            if (polyomino == null) return false;

            // 폴리오미노에서 셀 좌표 가져오기(사용처에서 보장)
            if (!TryGetCells(polyomino, stateIndex, out var cells)) return false;

            // 경계/겹침 체크
            foreach (var c in cells)
            {
                int gx = ax + c.x;
                int gy = ay + c.y;
                if (gx < 0 || gy < 0 || gx >= Width || gy >= Height) return false;

                if (TryGetPlacementAt(gx, gy, out _)) return false; // 세포 단위 겹침 금지(간단 정책)
            }

            placement = new Placement { polyomino = polyomino, stateIndex = stateIndex, x = ax, y = ay };
            _placements.Add(placement);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>해당 그리드 좌표에 존재하는 배치를 찾는다(첫 매칭).</summary>
        public bool TryGetPlacementAt(int gx, int gy, out Placement placement)
        {
            foreach (var p in _placements)
            {
                if (!TryGetCells(p.polyomino, p.stateIndex, out var cells)) continue;
                foreach (var c in cells)
                {
                    if (p.x + c.x == gx && p.y + c.y == gy)
                    {
                        placement = p;
                        return true;
                    }
                }
            }
            placement = default;
            return false;
        }

        /// <summary>배치 제거(좌표 기준).</summary>
        public bool TryRemoveAt(int gx, int gy, out Placement removed)
        {
            for (int i = 0; i < _placements.Count; i++)
            {
                var p = _placements[i];
                if (!TryGetCells(p.polyomino, p.stateIndex, out var cells)) continue;
                foreach (var c in cells)
                {
                    if (p.x + c.x == gx && p.y + c.y == gy)
                    {
                        removed = p;
                        _placements.RemoveAt(i);
                        OnChanged?.Invoke();
                        return true;
                    }
                }
            }
            removed = default;
            return false;
        }

        /// <summary>배치 나열(읽기전용 스냅샷 용).</summary>
        public IReadOnlyList<Placement> Placements => _placements;

        /// <summary>배치에서 폴리오미노 원본 꺼내기.</summary>
        public object getPolyomino(in Placement p) => p.polyomino;

        // --- Polyomino로부터 셀을 얻는 어댑터(구현체에 맞게 수정) ---
        private static bool TryGetCells(object poly, int state, out IReadOnlyList<Vector2Int> cells)
        {
            // 예시) Polyomino가 다음 시그니처를 가진다고 가정:
            // IReadOnlyList<Vector2Int> GetCells(int stateIndex)
            var m = poly.GetType().GetMethod("GetCells", new[] { typeof(int) });
            if (m != null)
            {
                cells = (IReadOnlyList<Vector2Int>)m.Invoke(poly, new object[] { state });
                return cells != null;
            }

            cells = null;
            return false;
        }
    }
}
