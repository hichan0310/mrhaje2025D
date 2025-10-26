using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public abstract class Polyomino:MonoBehaviour
    {
        // 추상 폴리오미노: 서브클래스가 기본 모양과 피봇만 정하면
        // 회전 상태(0/90/180/270)를 자동 사전계산합니다(대칭은 중복 제거).
        public sealed class State : IEquatable<State>
        {
            public readonly Cell[] Cells;  // 좌상단(0,0) 기준으로 정규화된 오프셋들
            public readonly int Width, Height;

            public State(Cell[] cells, int width, int height)
            {
                Cells = cells;
                Width = width;
                Height = height;
            }

            public bool Equals(State? other)
            {
                if (other is null || Width != other.Width || Height != other.Height || Cells.Length != other.Cells.Length) return false;
                for (int i = 0; i < Cells.Length; i++)
                    if (Cells[i].X != other.Cells[i].X || Cells[i].Y != other.Cells[i].Y) return false;
                return true;
            }
            public override bool Equals(object? obj) => obj is State s && Equals(s);
            public override int GetHashCode()
            {
                int h = Width * 31 + Height;
                foreach (var c in Cells) h = HashCode.Combine(h, c.X, c.Y);
                return h;
            }
        }

        public string Name { get; }
        public IReadOnlyList<State> States => _states;
        private readonly List<State> _states = new(4);

        protected Polyomino(string name)
        {
            Name = name;
            BuildStates();
            DedupSymmetricStates();
        }

        // 서브클래스가 기본 셀들과 피봇을 정의
        protected abstract (int x, int y)[] BaseCells();
        protected virtual (int x, int y) Pivot() => (0, 0);

        // 회전 상태 생성 + 좌상단 정규화(앵커를 "state의 좌상단"으로 사용할 수 있게)
        private void BuildStates()
        {
            var baseCells = BaseCells();
            var pivot = Pivot();

            for (int r = 0; r < 4; r++)
            {
                var tmp = new List<Cell>(baseCells.Length);
                foreach (var (x, y) in baseCells)
                {
                    int rx = x - pivot.x;
                    int ry = y - pivot.y;
                    // 시계 기준 회전
                    int nx, ny;
                    switch (r)
                    {
                        case 0: nx = rx;  ny = ry;  break;      // 0°
                        case 1: nx = ry;  ny = -rx; break;      // 90°
                        case 2: nx = -rx; ny = -ry; break;      // 180°
                        default: nx = -ry; ny = rx;  break;      // 270°
                    }
                    tmp.Add(new Cell(nx, ny));
                }

                // 좌상단(0,0)으로 당기기 + 정렬
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
                foreach (var c in tmp) { if (c.X < minX) minX = c.X; if (c.Y < minY) minY = c.Y; if (c.X > maxX) maxX = c.X; if (c.Y > maxY) maxY = c.Y; }
                int w = maxX - minX + 1, h = maxY - minY + 1;

                var norm = new Cell[tmp.Count];
                for (int i = 0; i < tmp.Count; i++)
                    norm[i] = new Cell(tmp[i].X - minX, tmp[i].Y - minY);

                Array.Sort(norm, (a, b) => a.Y != b.Y ? a.Y - b.Y : a.X - b.X);
                _states.Add(new State(norm, w, h));
            }
        }

        private void DedupSymmetricStates()
        {
            var uniq = new List<State>(4);
            foreach (var s in _states)
            {
                bool dup = false;
                foreach (var u in uniq) { if (u.Equals(s)) { dup = true; break; } }
                if (!dup) uniq.Add(s);
            }
            _states.Clear();
            _states.AddRange(uniq);
        }

        // 월드 좌표 셀들 (state 좌상단을 (ax, ay)로 앵커링)
        public IEnumerable<Cell> WorldCells(int stateIndex, int ax, int ay)
        {
            var s = _states[stateIndex % _states.Count];
            foreach (var c in s.Cells) yield return new Cell(ax + c.X, ay + c.Y);
        }
    }
}