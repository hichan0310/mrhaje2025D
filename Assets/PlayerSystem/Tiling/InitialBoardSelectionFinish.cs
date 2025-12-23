using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerSystem.Tiling
{
    public class InitialBoardSelectionFinish:MonoBehaviour, IPointerClickHandler
    {
        private InitialBoardSelection boardSelection;
        public InitialBoardSelection boardSelectionSet { set=>boardSelection=value; }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            boardSelection.finishSelection();
        }
    }
}