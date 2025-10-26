namespace PlayerSystem.Tiling
{
    // (x,y) 정수 좌표
    public readonly struct Cell
    {
        public readonly int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
    }
    
    
    // -------------------- 모양 서브클래스 예시 --------------------

    // 5칸 L 펜토미노
    public sealed class L5 : Polyomino
    {
        public L5() : base("L5") { }
        protected override (int x, int y)[] BaseCells() => new[]
        {
            (0,0),(0,1),(0,2),(0,3),(1,3)
        };
        // 회전 느낌을 조절하고 싶으면 Pivot을 바꾸세요. (좌상단 정규화로 최종 상태는 동일)
        protected override (int x, int y) Pivot() => (0,2);
    }

    // 6칸 I 헥소미노
    public sealed class I6 : Polyomino
    {
        public I6() : base("I6") { }
        protected override (int x, int y)[] BaseCells() => new[]
        {
            (0,0),(0,1),(0,2),(0,3),(0,4),(0,5)
        };
        protected override (int x, int y) Pivot() => (0,2);
    }

    // 6칸 'U' 형태 예시
    public sealed class U6 : Polyomino
    {
        public U6() : base("U6") { }
        protected override (int x, int y)[] BaseCells() => new[]
        {
            (0,0),(2,0),   // 윗변 양끝
            (0,1),(2,1),   // 중간 기둥
            (0,2),(1,2)    // 아랫변 일부 (6칸 예시)
        };
        protected override (int x, int y) Pivot() => (1,1);
    }

    // -------------------- 사용 예시 --------------------
    public static class Demo
    {
        public static void Run()
        {
            var board = new Board(width: 10, height: 8);

            Polyomino l5 = new L5();
            Polyomino i6 = new I6();
            Polyomino u6 = new U6();

            // (stateIndex: 0/1/2/3은 0/90/180/270°, 대칭이면 일부만 존재)
            if (board.TryPlace(l5, stateIndex: 1, ax: 1, ay: 0, out var p1))
            {
                // 배치 성공
            }

            if (!board.TryPlace(i6, stateIndex: 0, ax: 1, ay: 0, out _))
            {
                // 겹침/범위초과로 실패 → 다른 위치/회전 재시도
                board.TryPlace(i6, stateIndex: 1, ax: 4, ay: 1, out var p2);
            }

            // 되돌리기(백트래킹 등에서 사용)
            // board.Remove(p1);

            // 보드 채우기 퍼즐은 이 TryPlace/Remove를 이용해
            // 백트래킹이나 Exact Cover(DLX)로 조합 탐색하면 됩니다.
        }
    }
}
