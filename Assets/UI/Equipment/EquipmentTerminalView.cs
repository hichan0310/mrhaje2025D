using System;
using System.Collections.Generic;
using DefaultNamespace;
using PlayerSystem.Equipment;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Frontend
{
    [RequireComponent(typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster))]
    public sealed class EquipmentTerminalView : MonoBehaviour
    {
        private static readonly Color Cyan=new(.35f,.86f,.95f), Amber=new(1,.69f,.32f), Ink=new(.89f,.94f,.96f), Muted=new(.43f,.57f,.63f), Dark=new(.025f,.047f,.065f,.97f);
        private EquipmentLoadout loadout;
        private TMP_FontAsset font;
        private RectTransform screen, frame, content, ghost;
        private TextMeshProUGUI hint, status, count, itemTitle, itemBody, itemBonus, destination, installText;
        private TerminalPlate[] plates=new TerminalPlate[6];
        private readonly TextMeshProUGUI[] triggerTitles=new TextMeshProUGUI[6],effectTitles=new TextMeshProUGUI[6],powers=new TextMeshProUGUI[6],bonuses=new TextMeshProUGUI[6],states=new TextMeshProUGUI[6];
        private readonly Image[] triggerIcons=new Image[6],effectIcons=new Image[6];
        private readonly TerminalGlyph[] triggerGlyphs=new TerminalGlyph[6],effectGlyphs=new TerminalGlyph[6];
        private readonly List<GameObject> itemViews=new();
        private EquipmentKind tab;
        private int selectedSlot;
        private EquipmentItem selected, dragged;
        private Button install, remove;
        private TerminalPlate triggerTab,effectTab;
        private TimeScaler scaler;
        private bool wasCursorVisible;
        private CursorLockMode oldCursorLock;
        public bool IsOpen { get; private set; }
        public EquipmentItem Dragged => dragged;
        public EquipmentLoadout Loadout => loadout;

        public void Bind(EquipmentLoadout model,TMP_FontAsset face)
        {
            loadout=model;font=face;Build();
            loadout.Changed+=Refresh;Refresh();screen.gameObject.SetActive(false);
        }
        public void Toggle(){if(IsOpen)Close();else Open();}
        public void Open()
        {
            if(IsOpen || !loadout.Owner || loadout.Owner.stat.nowHp<=0 || loadout.Owner.ControlsLocked)return;
            loadout.Owner.FlushPendingCombatEvents();
            IsOpen=true;wasCursorVisible=Cursor.visible;oldCursorLock=Cursor.lockState;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            scaler=TimeScaler.Instance;
            if(!scaler)scaler=new GameObject("Time scaler").AddComponent<TimeScaler>();
            scaler.SetFactor(this,0);loadout.Owner.AcquireControlLock(this);
            screen.gameObject.SetActive(true);hint.gameObject.SetActive(false);Refresh();
        }
        public void Close()
        {
            if(!IsOpen)return;IsOpen=false;
            if(scaler)scaler.RemoveFactor(this);
            if(loadout!=null && loadout.Owner)loadout.Owner.ReleaseControlLock(this);
            Cursor.visible=wasCursorVisible;Cursor.lockState=oldCursorLock;
            if(loadout!=null && loadout.Owner)loadout.Owner.SuppressInputForFrame();
            if(screen)screen.gameObject.SetActive(false);
            if(hint)hint.gameObject.SetActive(true);EndDrag();
        }
        private void Update()
        {
            if(!IsOpen)return;
            for(int i=0;i<6;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))SelectSlot(i,tab);
            if(Input.GetKeyDown(KeyCode.Delete))UnequipSelected();
        }
        private void OnDisable()=>Close();
        private void OnDestroy(){Close();if(loadout!=null)loadout.Changed-=Refresh;}

        private void Build()
        {
            var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
            var scale=GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            if(!EventSystem.current)new GameObject("Equipment input",typeof(EventSystem),typeof(StandaloneInputModule));
            else if(!EventSystem.current.GetComponent<BaseInputModule>())EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            hint=Text("EquipmentHint",transform,"ESC  장비 결합",14,new Vector2(0,0),new Vector2(180,26),Muted);
            var hr=hint.rectTransform;hr.anchorMin=hr.anchorMax=new Vector2(1,0);hr.pivot=new Vector2(1,0);hr.anchoredPosition=new Vector2(-28,24);hint.alignment=TextAlignmentOptions.Right;
            screen=Rect("EquipmentScreen",transform);Stretch(screen);
            var dim=screen.gameObject.AddComponent<Image>();dim.color=new Color(.009f,.019f,.028f,.96f);
            // Engraved grid and a narrow amber datum line establish a shipboard equipment terminal.
            for(int x=0;x<1920;x+=80)Image("Grid",screen,new Vector2(x,0),new Vector2(1,1080),new Color(.12f,.25f,.3f,.12f));
            for(int y=0;y<1080;y+=80)Image("Grid",screen,new Vector2(0,-y),new Vector2(1920,1),new Color(.12f,.25f,.3f,.12f));
            frame=Rect("Terminal",screen);frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f);frame.pivot=new Vector2(.5f,.5f);frame.sizeDelta=new Vector2(1776,960);
            Image("Datum",frame,new Vector2(0,0),new Vector2(5,76),Amber);
            Text("Kicker",frame,"FIELD SYSTEM  /  신경 접속 장치",12,new Vector2(22,-2),new Vector2(700,20),Muted);
            var title=Text("Title",frame,"장비 결합",38,new Vector2(20,-26),new Vector2(700,52),Ink);title.fontStyle=FontStyles.Bold;
            Text("Intro",frame,"트리거가 반응하면, 연결한 효과가 발동합니다.",17,new Vector2(322,-42),new Vector2(800,30),Muted);
            Button("Close",frame,"ESC   전투 복귀",new Vector2(1588,-22),new Vector2(188,48),Close);
            Image("HeaderRule",frame,new Vector2(0,-108),new Vector2(1776,1),new Color(.2f,.36f,.42f));
            Text("CircuitHeading",frame,"결합 회로",20,new Vector2(0,-136),new Vector2(250,30),Ink);
            count=Text("CircuitCount",frame,"",14,new Vector2(720,-140),new Vector2(270,24),Cyan);count.alignment=TextAlignmentOptions.Right;
            for(int i=0;i<6;i++)BuildSlot(i,new Vector2((i%2)*508,-178-(i/2)*222));
            Text("CircuitNote",frame,"트리거 → 효과 연결을 완성하면 전투에 적용됩니다.\n트리거의 무작위 보너스는 결합한 회로에만 적용됩니다.",15,new Vector2(0,-868),new Vector2(1000,60),Muted);
            var trigger=Button("TriggerTab",frame,"트리거 보관함",new Vector2(1054,-136),new Vector2(350,46),()=>SwitchTab(EquipmentKind.Trigger));triggerTab=trigger.GetComponent<TerminalPlate>();
            var effect=Button("EffectTab",frame,"효과 보관함",new Vector2(1416,-136),new Vector2(360,46),()=>SwitchTab(EquipmentKind.Effect));effectTab=effect.GetComponent<TerminalPlate>();
            var viewport=Rect("InventoryViewport",frame);Place(viewport,new Vector2(1054,-200),new Vector2(722,410));viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=36;
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,.01f);
            content=Rect("InventoryContent",viewport);Place(content,Vector2.zero,new Vector2(722,410));scroll.content=content;scroll.viewport=viewport;
            var detail=Plate("ItemDetail",frame,new Vector2(1054,-634),new Vector2(722,248),Dark,new Color(.2f,.33f,.39f));
            destination=Text("Destination",detail.transform,"",12,new Vector2(22,-14),new Vector2(665,22),Cyan);
            itemTitle=Text("ItemTitle",detail.transform,"",23,new Vector2(22,-42),new Vector2(665,36),Ink);
            itemBody=Text("ItemDescription",detail.transform,"",15,new Vector2(22,-88),new Vector2(665,96),Muted);itemBody.enableWordWrapping=true;itemBody.alignment=TextAlignmentOptions.TopLeft;
            itemBonus=Text("ItemBonus",detail.transform,"",14,new Vector2(22,-195),new Vector2(665,34),Amber);itemBonus.enableWordWrapping=true;
            install=Button("Install",frame,"설치",new Vector2(1054,-900),new Vector2(498,48),InstallSelected);installText=install.GetComponentInChildren<TextMeshProUGUI>();
            remove=Button("Unequip",frame,"장착 해제",new Vector2(1568,-900),new Vector2(208,48),UnequipSelected);
            status=Text("Status",frame,"",14,new Vector2(0,-934),new Vector2(1030,26),Amber);
            var keys=Text("KeyHints",screen,"1–6 슬롯 선택     드래그하여 설치     우클릭 / DEL 해제     마우스 휠 보관함 스크롤",12,Vector2.zero,new Vector2(1776,24),Muted);
            keys.rectTransform.anchorMin=keys.rectTransform.anchorMax=new Vector2(.5f,0);keys.rectTransform.pivot=new Vector2(.5f,0);keys.rectTransform.anchoredPosition=new Vector2(0,12);
        }

        private void BuildSlot(int index,Vector2 position)
        {
            var plate=Plate("Circuit"+(index+1),frame,position,new Vector2(486,204),Dark,new Color(.18f,.29f,.35f));plates[index]=plate;
            var select=plate.gameObject.AddComponent<Button>();select.targetGraphic=plate;select.onClick.AddListener(()=>SelectSlot(index,tab));
            var drop=plate.gameObject.AddComponent<EquipmentSocketInput>();drop.View=this;drop.Slot=index;drop.AcceptBoth=true;
            Text("Number",plate.transform,(index+1).ToString("D2"),14,new Vector2(18,-12),new Vector2(50,22),Muted);
            states[index]=Text("ConnectionState",plate.transform,"",11,new Vector2(310,-14),new Vector2(154,20),Muted);states[index].alignment=TextAlignmentOptions.Right;
            var t=Socket(plate.transform,new Vector2(18,-44),new Vector2(450,62),index,EquipmentKind.Trigger);
            triggerGlyphs[index]=Glyph(t.transform,new Vector2(0,0),new Vector2(46,46),index,Cyan);
            triggerIcons[index]=Image("TriggerIcon",t.transform,Vector2.zero,new Vector2(46,46),Color.white);triggerIcons[index].preserveAspect=true;
            triggerTitles[index]=Text("TriggerName",t.transform,"",21,new Vector2(64,-1),new Vector2(362,28),Ink);
            powers[index]=Text("Power",t.transform,"",12,new Vector2(64,-32),new Vector2(360,21),Cyan);
            Image("Connector",plate.transform,new Vector2(40,-103),new Vector2(1,16),new Color(.25f,.5f,.58f));
            Image("Connector",plate.transform,new Vector2(40,-118),new Vector2(24,1),new Color(.25f,.5f,.58f));
            var e=Socket(plate.transform,new Vector2(22,-125),new Vector2(446,40),index,EquipmentKind.Effect);
            effectGlyphs[index]=Glyph(e.transform,Vector2.zero,new Vector2(36,36),2,Amber);
            effectIcons[index]=Image("EffectIcon",e.transform,Vector2.zero,new Vector2(36,36),Color.white);effectIcons[index].preserveAspect=true;
            effectTitles[index]=Text("EffectName",e.transform,"",19,new Vector2(60,0),new Vector2(362,32),Amber);
            bonuses[index]=Text("TriggerBonus",plate.transform,"",12,new Vector2(82,-174),new Vector2(384,20),Muted);
        }
        private GameObject Socket(Transform parent,Vector2 position,Vector2 size,int slot,EquipmentKind kind)
        {
            var rect=Rect("Socket",parent);Place(rect,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(1,1,1,.015f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>SelectSlot(slot,kind));
            var drop=rect.gameObject.AddComponent<EquipmentSocketInput>();drop.View=this;drop.Slot=slot;drop.Kind=kind;return rect.gameObject;
        }
        public void SelectSlot(int index,EquipmentKind kind){bool changed=tab!=kind;selectedSlot=index;tab=kind;selected=null;Refresh();if(changed)content.anchoredPosition=Vector2.zero;}
        private void SwitchTab(EquipmentKind kind){tab=kind;selected=null;Refresh();content.anchoredPosition=Vector2.zero;}
        public void SelectItem(EquipmentItem item){selected=item;if(item!=null)tab=item.Definition.kind;Refresh();}
        public void InstallSelected(){if(selected==null)return;Equip(selectedSlot,selected);}
        public void Equip(int index,EquipmentItem item)
        {
            if(loadout.TryEquip(index,item,out string reason)){selectedSlot=index;status.text=$"슬롯 {index+1:D2} · {item.Definition.title} 설치 완료";}
            else status.text=reason;
            EndDrag();Refresh();
        }
        public void UnequipSelected(){bool linked=loadout.Slots[selectedSlot].IsComplete && tab==EquipmentKind.Trigger;loadout.Unequip(selectedSlot,tab);selected=null;status.text=linked?"트리거와 연결된 효과를 보관함으로 돌려보냈습니다.":"해제한 장비를 보관함으로 돌려보냈습니다.";Refresh();}
        public void RemoveAt(int slot,EquipmentKind kind){selectedSlot=slot;tab=kind;UnequipSelected();}

        public void Refresh()
        {
            if(!frame)return;
            int complete=0;
            for(int i=0;i<6;i++){
                var slot=loadout.Slots[i];if(slot.IsComplete)complete++;
                plates[i].SetStyle(i==selectedSlot?new Color(.04f,.09f,.115f,.98f):Dark,i==selectedSlot?Cyan:new Color(.18f,.29f,.35f));
                triggerTitles[i].text=slot.Trigger?.Definition.title??"트리거 설치";
                triggerTitles[i].color=slot.Trigger!=null?Ink:Muted;
                powers[i].text=slot.Trigger?.Definition.powerSummary??"보관함에서 트리거를 선택하세요";
                effectTitles[i].text=slot.Effect?.Definition.title??(slot.Trigger!=null?"효과 연결":"트리거를 먼저 설치하세요");
                effectTitles[i].color=slot.Effect!=null?Amber:Muted;
                bonuses[i].text=slot.Trigger?.BonusDescription??"";
                bonuses[i].color=slot.IsComplete?Muted:new Color(.3f,.4f,.44f);
                states[i].text=slot.IsComplete?(slot.ActivationCount>0?$"발동 {slot.ActivationCount}회 · P {slot.LastPower:0.##}":"회로 연결됨"):slot.Trigger!=null?"효과 연결 대기":"빈 슬롯";states[i].color=slot.IsComplete?Cyan:Muted;
                SetIcon(triggerIcons[i],triggerGlyphs[i],slot.Trigger);SetIcon(effectIcons[i],effectGlyphs[i],slot.Effect);
            }
            count.text=$"연결 완료  {complete} / 06";
            triggerTab.GetComponentInChildren<TextMeshProUGUI>().text=$"트리거 보관함   {loadout.Items.FindAll(i=>i.Definition.kind==EquipmentKind.Trigger).Count}";
            effectTab.GetComponentInChildren<TextMeshProUGUI>().text=$"효과 보관함   {loadout.Items.FindAll(i=>i.Definition.kind==EquipmentKind.Effect).Count}";
            triggerTab.SetStyle(tab==EquipmentKind.Trigger?new Color(.04f,.12f,.15f):Dark,tab==EquipmentKind.Trigger?Cyan:Muted*.4f);
            effectTab.SetStyle(tab==EquipmentKind.Effect?new Color(.14f,.09f,.045f):Dark,tab==EquipmentKind.Effect?Amber:Muted*.4f);
            RebuildInventory();Details();
        }
        private static void SetIcon(Image image,TerminalGlyph glyph,EquipmentItem item)
        {
            glyph.Symbol=item?.Definition.glyph??-1;glyph.SetVerticesDirty();
            image.sprite=item?.Definition.icon;image.enabled=image.sprite;
            glyph.gameObject.SetActive(!image.enabled);glyph.color=item!=null?(item.Definition.kind==EquipmentKind.Trigger?Cyan:Amber):new Color(.24f,.35f,.39f);
        }
        private void RebuildInventory()
        {
            foreach(var go in itemViews){go.SetActive(false);Destroy(go);}itemViews.Clear();
            int row=0;
            foreach(var item in loadout.Items){
                if(item.Definition.kind!=tab)continue;
                var current=item;var tint=tab==EquipmentKind.Trigger?Cyan:Amber;
                var plate=Plate("InventoryItem",content,new Vector2(0,-row*78),new Vector2(722,68),selected==item?new Color(.08f,.14f,.17f):Dark,selected==item?tint:new Color(.13f,.23f,.28f));
                var button=plate.gameObject.AddComponent<Button>();button.targetGraphic=plate;button.onClick.AddListener(()=>SelectItem(current));
                var input=plate.gameObject.AddComponent<EquipmentItemInput>();input.View=this;input.Item=item;
                var glyph=Glyph(plate.transform,new Vector2(17,-13),new Vector2(40,40),loadout.Catalog?Array.IndexOf(loadout.Catalog.definitions,item.Definition):0,tint);
                var icon=Image("Icon",plate.transform,new Vector2(17,-13),new Vector2(40,40),Color.white);icon.preserveAspect=true;SetIcon(icon,glyph,item);
                Text("Name",plate.transform,item.Definition.title,18,new Vector2(76,-9),new Vector2(450,27),Ink);
                Text("Summary",plate.transform,tab==EquipmentKind.Trigger?item.Definition.powerSummary:"연결된 트리거의 파워로 발동",12,new Vector2(76,-40),new Vector2(450,20),Muted);
                int equipped=loadout.EquippedAt(item);
                var state=Text("Ownership",plate.transform,equipped>=0?$"슬롯 {equipped+1:D2}":"보관 중",12,new Vector2(570,-12),new Vector2(130,22),equipped>=0?tint:Muted);state.alignment=TextAlignmentOptions.Right;
                if(tab==EquipmentKind.Trigger)Text("Bonus",plate.transform,item.BonusDescription,11,new Vector2(380,-43),new Vector2(320,18),tint).alignment=TextAlignmentOptions.Right;
                itemViews.Add(plate.gameObject);row++;
            }
            if(row==0){var empty=Text("EmptyInventory",content,"보관 중인 장비가 없습니다.\n탐색 중 획득한 장비가 여기에 보관됩니다.",17,new Vector2(30,-120),new Vector2(660,100),Muted);empty.enableWordWrapping=true;empty.alignment=TextAlignmentOptions.Center;itemViews.Add(empty.gameObject);}
            content.sizeDelta=new Vector2(722,Mathf.Max(410,row*78));
        }
        private void Details()
        {
            var equipped=tab==EquipmentKind.Trigger?loadout.Slots[selectedSlot].Trigger:loadout.Slots[selectedSlot].Effect;
            var item=selected??equipped;
            destination.text=$"슬롯 {selectedSlot+1:D2}   /   {(tab==EquipmentKind.Trigger?"트리거 입력":"효과 출력")}";
            itemTitle.text=item?.Definition.title??(tab==EquipmentKind.Trigger?"어떤 순간에 발동할까요?":"어떤 효과를 연결할까요?");
            itemBody.text=item?.Definition.description??(tab==EquipmentKind.Trigger?"오른쪽 보관함에서 트리거를 선택해 설치하세요.\n회피, 일반 공격, 스킬 등 원하는 발동 조건을 고를 수 있습니다.":"설치한 트리거가 발동하면 연결한 효과를 실행합니다.\n트리거의 파워에 따라 효과의 강도가 결정됩니다.");
            itemBonus.text=item==null?"":item.Definition.kind==EquipmentKind.Trigger?"장착 보너스  /  "+item.BonusDescription:"효과에는 무작위 능력치 보너스가 붙지 않습니다.";
            install.interactable=selected!=null && loadout.EquippedAt(selected)!=selectedSlot && (tab==EquipmentKind.Trigger || loadout.Slots[selectedSlot].Trigger!=null);
            installText.text=selected!=null && loadout.EquippedAt(selected)>=0?$"슬롯 {selectedSlot+1:D2}로 이동 / 교환":$"슬롯 {selectedSlot+1:D2}에 설치";
            remove.interactable=equipped!=null;remove.GetComponentInChildren<TextMeshProUGUI>().text=$"슬롯 {selectedSlot+1:D2} 해제";
        }
        public void BeginDrag(EquipmentItem item)
        {
            dragged=item;selected=item;
            if(ghost)Destroy(ghost.gameObject);
            var plate=Plate("DraggedEquipment",transform,Vector2.zero,new Vector2(252,46),Dark,item.Definition.kind==EquipmentKind.Trigger?Cyan:Amber);ghost=plate.rectTransform;
            var group=ghost.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
            Text("Name",ghost,item.Definition.title,16,new Vector2(14,-8),new Vector2(224,30),Ink);MoveDrag();
        }
        public void MoveDrag(){if(!ghost)return;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,Input.mousePosition,null,out var point);ghost.anchorMin=ghost.anchorMax=new Vector2(.5f,.5f);ghost.anchoredPosition=point+new Vector2(18,-18);}
        public void EndDrag(){dragged=null;if(ghost){ghost.gameObject.SetActive(false);Destroy(ghost.gameObject);}ghost=null;}

        private RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static void Place(RectTransform r,Vector2 p,Vector2 size){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=p;r.sizeDelta=size;}
        private Image Image(string name,Transform parent,Vector2 p,Vector2 size,Color color){var r=Rect(name,parent);Place(r,p,size);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        private TerminalPlate Plate(string name,Transform parent,Vector2 p,Vector2 size,Color fill,Color border){var r=Rect(name,parent);Place(r,p,size);var g=r.gameObject.AddComponent<TerminalPlate>();g.SetStyle(fill,border);return g;}
        private TerminalGlyph Glyph(Transform parent,Vector2 p,Vector2 size,int symbol,Color color){var r=Rect("CircuitGlyph",parent);Place(r,p,size);var g=r.gameObject.AddComponent<TerminalGlyph>();g.Symbol=Mathf.Max(0,symbol);g.color=color;g.raycastTarget=false;return g;}
        private TextMeshProUGUI Text(string name,Transform parent,string value,float size,Vector2 p,Vector2 dimensions,Color color){var r=Rect(name,parent);Place(r,p,dimensions);var t=r.gameObject.AddComponent<TextMeshProUGUI>();if(font)t.font=font;t.text=value;t.fontSize=size;t.color=color;t.enableWordWrapping=false;t.overflowMode=TextOverflowModes.Ellipsis;t.alignment=TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;return t;}
        private Button Button(string name,Transform parent,string label,Vector2 p,Vector2 size,Action action){var plate=Plate(name,parent,p,size,Dark,new Color(.25f,.39f,.44f));var b=plate.gameObject.AddComponent<Button>();b.targetGraphic=plate;var colors=b.colors;colors.highlightedColor=new Color(.6f,.95f,1f);colors.pressedColor=new Color(.4f,.7f,.8f);colors.disabledColor=new Color(.35f,.4f,.42f);b.colors=colors;b.onClick.AddListener(()=>action());var t=Text("Label",plate.transform,label,16,Vector2.zero,size,Ink);t.alignment=TextAlignmentOptions.Center;return b;}
    }
}
