using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlayerSystem.Tiling
{
    public class InitialBoardSelectionCheckbox:MonoBehaviour, IPointerClickHandler
    {
        private InitialBoardSelection boardSelection;
        
        public InitialBoardSelection boardSelectionSet { set=>boardSelection=value; }
        [SerializeField] InitialBoardSelectionList boardSelectionList;
        [SerializeField] Image image;
        [SerializeField] Sprite imageSelected;
        [SerializeField] Sprite imageUnselected;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (boardSelection.selectBoard(this.boardSelectionList.board))
            {
                image.sprite = imageSelected;
            }
            else
            {
                image.sprite = imageUnselected;
            }
                
        }
    }
}