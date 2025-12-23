using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public class InitialBoardSelection : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;

        [SerializeField] private List<InitialBoardSelectionList> boards;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private InitialBoardSelectionFinish finish;

        private List<Board> selectedBoards = new List<Board>();

        private void Start()
        {
            foreach (var b in boards)
            {
                b.boardSelectionSet = this;
            }
            finish.boardSelectionSet = this;
        }

        public void setText(Board board)
        {
            nameText.text = board.name;
            descriptionText.text = board.Description;
        }

        public bool selectBoard(Board board)
        {
            if (selectedBoards.Contains(board)) selectedBoards.Remove(board);
            else if (selectedBoards.Count < 5) selectedBoards.Add(board);
            return selectedBoards.Contains(board);
        }

        public void finishSelection()
        {
            inventory.gameObject.SetActive(true);
            inventory.show = true;
            for (int i = 0; i < selectedBoards.Count; i++)
            {
                selectedBoards[i] = Instantiate(selectedBoards[i]);
                selectedBoards[i].transform.SetParent(inventory.transform);
                selectedBoards[i].GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
                // Debug.Log(selectedBoards[i].name);
            }
            inventory.setBoards(selectedBoards);
            Destroy(this.gameObject);
        }
    }
}