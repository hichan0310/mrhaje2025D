using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;

namespace PlayerSystem.Tiling
{
    public sealed class Board
    {
        public readonly int W, H;
        private readonly ulong[] rows;
        private readonly List<Polyomino> _catalog;

        // 배치된 ITriggerEffect와 내부 쿨다운
        private sealed class ActiveEffect
        {
            public ITriggerEffect Effect;
            public float CooldownRemaining;
            public ActiveEffect(ITriggerEffect fx) { Effect = fx; CooldownRemaining = 0f; }
        }
        private readonly List<ActiveEffect> _activeEffects = new();

        public Board(int width, int height, IEnumerable<Polyomino>? catalog = null)
        {
            if (width < 1 || height < 1) throw new ArgumentException("Invalid board size");
            if (width > 64) throw new ArgumentException("Width up to 64 supported (use bool[,] if wider).");
            W = width; H = height;
            rows = new ulong[H];
            _catalog = catalog != null ? new List<Polyomino>(catalog) : new List<Polyomino>();
        }

        public void RegisterPolyomino(Polyomino p) => _catalog.Add(p);
        public bool InBounds(int x, int y) => (uint)x < W && (uint)y < H;
        public bool IsFilled(int x, int y) => InBounds(x, y) && (((rows[y] >> x) & 1UL) != 0UL);

        public readonly struct Placement
        {
            public readonly Polyomino? Poly;
            public readonly int StateIndex;
            public readonly int AnchorX, AnchorY;
            public readonly (int x, int y)[] Cells;
            public bool IsValid => Cells != null && Cells.Length > 0;
            public Placement(Polyomino? poly, int stateIndex, int ax, int ay, (int x, int y)[] cells)
            { Poly = poly; StateIndex = stateIndex; AnchorX = ax; AnchorY = ay; Cells = cells; }
        }

        public bool Fits(Polyomino poly, int stateIndex, int ax, int ay)
        {
            foreach (var c in poly.WorldCells(stateIndex, ax, ay))
            {
                if (!InBounds(c.X, c.Y)) return false;
                if (((rows[c.Y] >> c.X) & 1UL) != 0UL) return false;
            }
            return true;
        }

        public bool TryPlace(Polyomino poly, int stateIndex, int ax, int ay, out Placement placement)
        {
            placement = default;
            if (!Fits(poly, stateIndex, ax, ay)) return false;

            var list = new List<(int x, int y)>();
            foreach (var c in poly.WorldCells(stateIndex, ax, ay))
            {
                rows[c.Y] |= (1UL << c.X);
                list.Add((c.X, c.Y));
            }
            placement = new Placement(poly, stateIndex, ax, ay, list.ToArray());

            // 폴리오미노가 ITriggerEffect라면 활성 목록에 등록
            if (poly is ITriggerEffect fx)
                _activeEffects.Add(new ActiveEffect(fx));

            return true;
        }

        public void Remove(in Placement placement)
        {
            if (!placement.IsValid) return;

            // 활성 이펙트 제거
            if (placement.Poly is ITriggerEffect fx)
            {
                for (int i = _activeEffects.Count - 1; i >= 0; i--)
                    if (ReferenceEquals(_activeEffects[i].Effect, fx))
                        _activeEffects.RemoveAt(i);
            }

            foreach (var (x, y) in placement.Cells)
                if ((uint)x < W && (uint)y < H)
                    rows[y] &= ~(1UL << x);
        }

        public bool TryGetPlacementAt(int sx, int sy, out Placement placement)
        {
            placement = default;
            if (!InBounds(sx, sy) || !IsFilled(sx, sy)) return false;

            var stack = new Stack<(int x, int y)>();
            var visited = new HashSet<(int, int)>();
            var cells = new List<(int x, int y)>();

            stack.Push((sx, sy));
            visited.Add((sx, sy));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                cells.Add((x, y));

                var neigh = new (int x, int y)[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) };
                foreach (var (nx, ny) in neigh)
                    if ((uint)nx < W && (uint)ny < H && IsFilled(nx, ny) && visited.Add((nx, ny)))
                        stack.Push((nx, ny));
            }

            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var (x, y) in cells) { if (x < minX) minX = x; if (y < minY) minY = y; }

            placement = new Placement(null, -1, minX, minY, cells.ToArray());
            return true;
        }

        public Polyomino? getPolyomino(in Placement place)
        {
            if (place.Poly != null) return place.Poly;
            if (!place.IsValid) return null;

            var norm = Normalize(place.Cells);
            foreach (var poly in _catalog)
            {
                var states = poly.States;
                for (int i = 0; i < states.Count; i++)
                {
                    var s = states[i];
                    if (s.Cells.Length != norm.Length) continue;
                    if (SameShape(s.Cells, norm)) return poly;
                }
            }
            return null;
        }

        private static Cell[] Normalize((int x, int y)[] cells)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            for (int i = 0; i < cells.Length; i++)
            { var (x, y) = cells[i]; if (x < minX) minX = x; if (y < minY) minY = y; }

            var n = new Cell[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            { var (x, y) = cells[i]; n[i] = new Cell(x - minX, y - minY); }

            Array.Sort(n, (a, b) => a.Y != b.Y ? a.Y - b.Y : a.X - b.X);
            return n;
        }

        private static bool SameShape(Cell[] stateCells, Cell[] norm)
        {
            if (stateCells.Length != norm.Length) return false;
            for (int i = 0; i < norm.Length; i++)
                if (stateCells[i].X != norm[i].X || stateCells[i].Y != norm[i].Y) return false;
            return true;
        }

        // 내부 쿨다운 갱신
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].CooldownRemaining > 0f)
                    _activeEffects[i].CooldownRemaining = Math.Max(0f, _activeEffects[i].CooldownRemaining - deltaTime);
            }
        }

        // 이벤트 → 보드 위 이펙트 실행
        public void OnEvent(EntitySystem.Events.EventArgs e, Entity target, Func<EntitySystem.Events.EventArgs, float?> powerSel, float internalCooldownSec = 0f)
        {
            if (e == null || target == null || powerSel == null) return;

            var p = powerSel(e);
            if (p is not float power || power <= 0f) return;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var a = _activeEffects[i];
                if (a.CooldownRemaining > 0f) continue;

                a.Effect.trigger(target, power); // ITriggerEffect(프로젝트 시그니처 유지)
                a.CooldownRemaining = Math.Max(0f, internalCooldownSec);
            }
        }
    }
}
