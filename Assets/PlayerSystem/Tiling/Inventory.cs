using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public class Inventory : MonoBehaviour, IGetBoardItem, IGetBagItem
    {
        [SerializeField] private List<Board> boards = new List<Board>();
        [SerializeField] private Bag bag;
        private int boardIndex = 0;

        public bool show { get; set; } = true;
        
        public void close()
        {
            this.show = false;
        }


        private void Start()
        {
            bag.getBagItem = this;
            foreach (var board in boards) board.getBoardItem = this;
        }

        public void addBoard(Board board)
        {
            this.boards.Add(board);
            board.getBoardItem = this;
        }

        private void Update()
        {
            if (show)
            {
                bag.show = true;
                bag.gameObject.SetActive(true);
                for (int i = 0; i < boards.Count; i++)
                {
                    
                    if (i == boardIndex)
                    {
                        boards[i].show = true;
                        boards[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        boards[i].show = false;
                    }
                }
            }
            else
            {
                bag.show = false;
                foreach (var b in boards) b.show = false;
                this.gameObject.SetActive(false);
            }
        }

        void IGetBoardItem.somethingSelected(Polyomino selected)
        {
            if (show) boards[boardIndex].selected = selected;
            else bag.addBagItem(selected);
        }

        void IGetBagItem.somethingSelected(Polyomino selected)
        {
            bag.addBagItem(boards[boardIndex].selected);
            boards[boardIndex].selected = selected;
        }

        public void setIndex(int index)
        {
            this.boardIndex = Math.Clamp(index, 0, boards.Count - 1);
        }
    }
}