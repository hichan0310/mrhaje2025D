using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerSystem.Tiling
{
    public class InitialBoardSelectionList:MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Board boardPrefab;
        private InitialBoardSelection boardSelection;
        
        public Board board{get=>boardPrefab;}
        
        public InitialBoardSelection boardSelectionSet { set=>boardSelection=value; }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            boardSelection.setText(this.board);
        }
    }
}