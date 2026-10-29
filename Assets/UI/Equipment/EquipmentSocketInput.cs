using PlayerSystem.Equipment;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Frontend
{
    public sealed class EquipmentSocketInput : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public EquipmentTerminalView View;
        public int Slot;
        public EquipmentKind Kind;
        public bool AcceptBoth;
        public void OnDrop(PointerEventData e){if(View.Dragged!=null && (AcceptBoth || View.Dragged.Definition.kind==Kind))View.Equip(Slot,View.Dragged);}
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Right)View.RemoveAt(Slot,Kind);}
    }
}
