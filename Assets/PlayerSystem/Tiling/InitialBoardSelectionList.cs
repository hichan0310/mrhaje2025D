using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerSystem.Tiling
{
    public class InitialBoardSelectionList:MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Board boardPrefab;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private InitialBoardSelectionCheckbox checkbox;
        private InitialBoardSelection boardSelection;
        
        public Board board{get=>boardPrefab;}
        
        public InitialBoardSelection boardSelectionSet
        {
            set
            {
                checkbox.boardSelectionSet = value;
                boardSelection = value;
            }
        }

        private void Start()
        {
            nameText.text = boardPrefab.name;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            boardSelection.setText(this.board);
        }
    }
}