using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using TMPro;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public class Inventory : MonoBehaviour, IGetBoardItem, IGetBagItem
    {
        [SerializeField] private List<Board> boards = new List<Board>();
        [SerializeField] private Bag bag;
        [SerializeField] private TMP_Text textDescription;
        [SerializeField] private TMP_Text textName;
        public Entity entity { get; set; }
        private int boardIndex = 0;
        [SerializeField] private List<OnClickWithArgs1> boardSelections = new();

        public bool show = true;

        public void close()
        {
            this.show = false;
        }


        private void Start()
        {
            foreach (Board board in boards)
                board.registerTarget(this.entity);
            foreach (var bs in boardSelections)
                bs.gameObject.SetActive(false);
            for (int i = 0; i < boards.Count; i++)
                boardSelections[i].gameObject.SetActive(true);
            bag.getBagItem = this;
            foreach (var board in boards) board.getBoardItem = this;
        }

        public void addPolyomino(Polyomino polyomino)
        {
            this.bag.addBagItem(polyomino);
        }

        public void addBoard(Board b)
        {
            var board = Instantiate(b);
            this.boards.Add(board);
            board.getBoardItem = this;
            board.registerTarget(this.entity);

            this.boardSelections[this.boards.Count - 1].gameObject.SetActive(true);
        }


        public void removeBoard(Board board)
        {
            this.bag.addBagItem(board.reset());
            this.boards.Remove(board);
            board.removeSelf();
            Destroy(board.gameObject);
            this.boardSelections[this.boards.Count].gameObject.SetActive(false);
        }

        private void Update()
        {
            if (show)
            {
                bag.show = true;
                bag.gameObject.SetActive(true);
                this.textDescription.gameObject.SetActive(true);
                this.textName.gameObject.SetActive(true);
                bool selected = false;
                for (int i = 0; i < boards.Count; i++)
                {
                    if (i == boardIndex)
                    {
                        boards[i].show = true;
                        boards[i].gameObject.SetActive(true);
                        if (boards[i].selected)
                        {
                            this.textDescription.text = this.boards[i].selected.Description;
                            this.textName.text = this.boards[i].selected.Name;
                            selected = true;
                        }
                    }
                    else
                    {
                        boards[i].show = false;
                    }
                }

                if (!selected)
                {
                    this.textDescription.text = this.boards[this.boardIndex].Description;
                    this.textName.text = this.boards[this.boardIndex].Name;
                }
            }
            else
            {
                bag.show = false;
                foreach (var b in boards) b.show = false;
                this.gameObject.SetActive(false);
                this.textDescription.gameObject.SetActive(false);
                this.textName.gameObject.SetActive(false);
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