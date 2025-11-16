using System;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public class BoardAssign:MonoBehaviour
    {
        private Board board;

        private void Start()
        {
            this.board = GetComponent<Board>();
        }

        public void click(int x, int y)
        {
            this.board.click(x, y);
        }
    }
}