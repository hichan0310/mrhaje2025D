// Assets/PlayerSystem/Triggers/IBoardEditableTrigger.cs
using PlayerSystem.Tiling;

namespace PlayerSystem.Triggers
{
    /// <summary>보드 에디터가 트리거의 보드를 수정할 수 있게 해주는 최소 인터페이스</summary>
    public interface IBoardEditableTrigger
    {
        Board Board { get; } // 현재 보드
        bool TryAdd(object effectPolyomino, int stateIndex, int x, int y); // 배치
        bool TryRemove(int x, int y);                                        // 제거
    }
}
