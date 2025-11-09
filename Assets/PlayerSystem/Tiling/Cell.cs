using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlayerSystem.Tiling
{
    // (x,y) 정수 좌표
    [Serializable]
    public struct Cell
    {
        public int x, y;
    }
}