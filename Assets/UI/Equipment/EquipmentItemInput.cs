using PlayerSystem.Equipment;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Frontend
{
    public sealed class EquipmentItemInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public EquipmentTerminalView View;
        public EquipmentItem Item;
        public void OnBeginDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)View.BeginDrag(Item);}
        public void OnDrag(PointerEventData e)=>View.MoveDrag();
        public void OnEndDrag(PointerEventData e)=>View.EndDrag();
        public void OnPointerClick(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Right){int slot=View.Loadout.EquippedAt(Item);if(slot>=0)View.RemoveAt(slot,Item.Definition.kind);}
            else if(e.clickCount==2){View.SelectItem(Item);View.InstallSelected();}
        }
    }
}
