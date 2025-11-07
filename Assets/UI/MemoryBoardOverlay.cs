// Assets/UI/TestMemoryBoardOverlay.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PlayerSystem;

public class MemoryBoardOverlay : MonoBehaviour
{
    // 내부 UI 참조
    private Canvas _canvas;
    private RectTransform _root;
    private Text _titleText;
    private Button _prevBtn, _nextBtn;
    private Toggle _removeToggle;

    private RectTransform _gridRoot;
    private GridLayoutGroup _gridLayout;
    private readonly List<Button> _gridButtons = new();

    private RectTransform _inventoryRoot;
    private ScrollRect _inventoryScroll;
    private VerticalLayoutGroup _inventoryList;
    private readonly List<Button> _inventoryButtons = new();
    private bool _built;   
    private bool _open;
    // 데이터
    private PlayerMemoryBinder _binder;
    private MemoryBoard _board => _binder?.ActiveBoard;
    private PlayerMemoryBinder.MemoryPieceInventoryItem? _selectedItem;

    // 캐시 버퍼
    private readonly List<MemoryBoard.MemoryPiecePlacementInfo> _pieces = new();
    private readonly List<MemoryBoard.MemoryReinforcementInfo> _zones = new();

    private void Awake()
    {
        // 플레이어/바인더 자동 탐색
        _binder = FindObjectOfType<PlayerMemoryBinder>();
        if (_binder == null)
        {
            Debug.LogWarning("[Overlay] PlayerMemoryBinder not found in scene.");
            enabled = false;
            return;
        }

        EnsureEventSystem();
        BuildCanvas();
        BuildHeader();
        BuildGrid();
        BuildInventory();

        // 바인더 이벤트 구독
        _binder.BoardListChanged += OnBoardListChanged;
        _binder.ActiveBoardChanged += OnActiveBoardChanged;
        _binder.BoardChanged += OnBoardChanged;
        _binder.InventoryChanged += OnInventoryChanged;
        _built = true;
    }

    private void Update()
    {
        if (_open && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }


    private void OnDestroy()
    {
        if (_binder != null)
        {
            _binder.BoardListChanged -= OnBoardListChanged;
            _binder.ActiveBoardChanged -= OnActiveBoardChanged;
            _binder.BoardChanged -= OnBoardChanged;
            _binder.InventoryChanged -= OnInventoryChanged;
        }
    }

    private void Start()
    {
        // 초기 렌더
        RefreshHeader();
        RebuildGrid();
        RefreshGrid();
        RefreshInventory();
    }

    // ---------------- UI BUILD ----------------

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }

    private void BuildCanvas()
    {
        var go = new GameObject("TestMemoryBoardOverlay_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);

        _root = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
        _root.SetParent(_canvas.transform, false);
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

        // 뒤 배경 패널(반투명)
        var bg = CreateImage(_root, new Color(0, 0, 0, 0.3f));
        bg.name = "BackPanel";
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
    }

    private void BuildHeader()
    {
        // 상단 바
        var topBar = CreatePanel(_root, new Color(0.12f, 0.12f, 0.12f, 0.9f));
        var rt = topBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 64);
        rt.anchoredPosition = Vector2.zero;

        _titleText = CreateText(topBar.transform, "Board 0/0", 20, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
        var tRt = _titleText.rectTransform;
        tRt.anchorMin = new Vector2(0.3f, 0);
        tRt.anchorMax = new Vector2(0.7f, 1);
        tRt.offsetMin = tRt.offsetMax = Vector2.zero;

        _prevBtn = CreateButton(topBar.transform, "< Prev");
        var pRt = _prevBtn.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0, 0);
        pRt.anchorMax = new Vector2(0, 1);
        pRt.sizeDelta = new Vector2(120, 0);
        pRt.anchoredPosition = new Vector2(70, 0);
        _prevBtn.onClick.AddListener(() =>
        {
            if (_binder.BoardCount <= 0) return;
            var idx = (_binder.ActiveBoardIndex - 1 + _binder.BoardCount) % _binder.BoardCount;
            _binder.SetActiveBoard(idx);
        });

        _nextBtn = CreateButton(topBar.transform, "Next >");
        var nRt = _nextBtn.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(1, 0);
        nRt.anchorMax = new Vector2(1, 1);
        nRt.sizeDelta = new Vector2(120, 0);
        nRt.anchoredPosition = new Vector2(-70, 0);
        _nextBtn.onClick.AddListener(() =>
        {
            if (_binder.BoardCount <= 0) return;
            var idx = (_binder.ActiveBoardIndex + 1) % _binder.BoardCount;
            _binder.SetActiveBoard(idx);
        });

        _removeToggle = CreateToggle(topBar.transform, "Remove");
        var rRt = _removeToggle.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.15f, 0);
        rRt.anchorMax = new Vector2(0.15f, 1);
        rRt.sizeDelta = new Vector2(140, 0);
        rRt.anchoredPosition = Vector2.zero;
    }

    private void BuildGrid()
    {
        // 좌측 메인 그리드 영역
        var panel = CreatePanel(_root, new Color(0.18f, 0.18f, 0.18f, 0.9f));
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0.7f, 1);
        rt.offsetMin = new Vector2(16, 16);
        rt.offsetMax = new Vector2(-8, -80);

        _gridRoot = new GameObject("GridRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        _gridRoot.SetParent(panel.transform, false);
        _gridRoot.anchorMin = new Vector2(0, 0);
        _gridRoot.anchorMax = new Vector2(1, 1);
        _gridRoot.offsetMin = new Vector2(16, 16);
        _gridRoot.offsetMax = new Vector2(-16, -16);

        _gridLayout = _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        _gridLayout.spacing = new Vector2(4, 4);
        _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        _gridLayout.childAlignment = TextAnchor.UpperLeft;
        _gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
    }

    private void BuildInventory()
    {
        // 우측 인벤토리 영역
        var panel = CreatePanel(_root, new Color(0.12f, 0.12f, 0.12f, 0.9f));
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.7f, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(8, 16);
        rt.offsetMax = new Vector2(-16, -80);

        var title = CreateText(panel.transform, "Inventory", 18, TextAnchor.UpperCenter, FontStyle.Bold, Color.white);
        var tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0, 1);
        tRt.anchorMax = new Vector2(1, 1);
        tRt.pivot = new Vector2(0.5f, 1);
        tRt.sizeDelta = new Vector2(0, 30);
        tRt.anchoredPosition = new Vector2(0, -10);

        var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scrollGO.transform.SetParent(panel.transform, false);
        var sRt = (RectTransform)scrollGO.transform;
        sRt.anchorMin = new Vector2(0, 0);
        sRt.anchorMax = new Vector2(1, 1);
        sRt.offsetMin = new Vector2(8, 8);
        sRt.offsetMax = new Vector2(-8, -50);
        var sImg = scrollGO.GetComponent<Image>();
        sImg.color = new Color(1, 1, 1, 0.05f);
        var mask = scrollGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(scrollGO.transform, false);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;

        _inventoryScroll = scrollGO.GetComponent<ScrollRect>();
        _inventoryScroll.horizontal = false;
        _inventoryScroll.vertical = true;
        _inventoryScroll.viewport = sRt;
        _inventoryScroll.content = content;

        _inventoryList = content.gameObject.AddComponent<VerticalLayoutGroup>();
        _inventoryList.spacing = 6f;
        _inventoryList.childControlWidth = true;
        _inventoryList.childControlHeight = true;
        _inventoryList.childForceExpandWidth = true;
        _inventoryList.childForceExpandHeight = false;

        var footer = CreateText(panel.transform, "Click a cell to place / Toggle Remove to delete.", 12, TextAnchor.LowerCenter, FontStyle.Italic, new Color(0.9f, 0.9f, 0.9f, 0.9f));
        var fRt = footer.rectTransform;
        fRt.anchorMin = new Vector2(0, 0);
        fRt.anchorMax = new Vector2(1, 0);
        fRt.sizeDelta = new Vector2(0, 24);
        fRt.anchoredPosition = new Vector2(0, 12);
    }

    // ---------------- EVENTS ----------------

    private void OnBoardListChanged()
    {
        RefreshHeader();
        RebuildGrid();
        RefreshGrid();
    }

    private void OnActiveBoardChanged(int idx)
    {
        RefreshHeader();
        RebuildGrid();
        RefreshGrid();
    }

    private void OnBoardChanged(int idx)
    {
        if (idx == _binder.ActiveBoardIndex)
            RefreshGrid();
    }

    private void OnInventoryChanged()
    {
        RefreshInventory();
    }

    // ---------------- REFRESH ----------------

    private void RefreshHeader()
    {
        if (_titleText == null || _binder == null) return;

        var total = _binder.BoardCount;
        var current = Mathf.Max(0, _binder.ActiveBoardIndex + 1);
        _titleText.text = $"Board {current}/{Mathf.Max(0, total)} {(_removeToggle != null && _removeToggle.isOn ? "[Remove]" : "")}";
    }


    private void RebuildGrid()
    {
        foreach (var b in _gridButtons)
            if (b) Destroy(b.gameObject);
        _gridButtons.Clear();

        if (_board == null)
        {
            _gridLayout.cellSize = new Vector2(48, 48);
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = 4;
            return;
        }

        var gs = _board.GridSize;
        var cell = Mathf.FloorToInt(Mathf.Min(_gridRoot.rect.width / Mathf.Max(gs.x, 1), _gridRoot.rect.height / Mathf.Max(gs.y, 1)) - 6);
        cell = Mathf.Clamp(cell, 24, 72);

        _gridLayout.cellSize = new Vector2(cell, cell);
        _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _gridLayout.constraintCount = Mathf.Max(gs.x, 1);

        for (int y = gs.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < gs.x; x++)
            {
                var btn = CreateCellButton(_gridRoot, x, y);
                _gridButtons.Add(btn);
            }
        }
    }

    private void RefreshGrid()
    {
        if (_board == null) return;

        _pieces.Clear();
        _zones.Clear();
        _board.GetPiecePlacements(_pieces);
        _board.GetReinforcementPlacements(_zones);

        // 빠른 조회를 위해 점유 맵 구성
        var gs = _board.GridSize;
        var occ = new bool[gs.x, gs.y];
        var zone = new bool[gs.x, gs.y];

        foreach (var p in _pieces)
            foreach (var c in p.OccupiedCells)
                if (InBounds(c, gs)) occ[c.x, c.y] = true;

        foreach (var z in _zones)
            foreach (var c in z.OccupiedCells)
                if (InBounds(c, gs)) zone[c.x, c.y] = true;

        // 버튼 색 갱신
        int i = 0;
        for (int y = gs.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < gs.x; x++, i++)
            {
                var btn = _gridButtons[i];
                var img = btn.GetComponent<Image>();
                var isOcc = occ[x, y];
                var isZone = zone[x, y];

                Color col;
                if (isOcc && isZone) col = new Color(0.9f, 0.6f, 0.2f, 1f);   // 점유 & 강화
                else if (isOcc) col = new Color(0.2f, 0.7f, 1f, 1f);        // 점유
                else if (isZone) col = new Color(0.3f, 1f, 0.3f, 0.6f);      // 강화
                else col = new Color(1f, 1f, 1f, 0.12f);          // 빈칸

                img.color = col;
            }
        }
    }

    private void RefreshInventory()
    {
        foreach (var b in _inventoryButtons)
            if (b) Destroy(b.gameObject);
        _inventoryButtons.Clear();

        var inv = _binder.Inventory;
        for (int i = 0; i < inv.Count; i++)
        {
            var item = inv[i];
            var label = item.Asset ? $"{item.Asset.DisplayName} x{item.PowerMultiplier:0.##}" : "null";
            var btn = CreateListButton(_inventoryList.transform, label);
            int capture = i;
            btn.onClick.AddListener(() =>
            {
                // 선택 토글
                if (_selectedItem.HasValue && _selectedItem.Value.Asset == item.Asset && Mathf.Abs(_selectedItem.Value.PowerMultiplier - item.PowerMultiplier) < 0.0001f)
                {
                    _selectedItem = null;
                    btn.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
                }
                else
                {
                    _selectedItem = item;
                    // 전체 버튼 색 초기화
                    foreach (var b in _inventoryButtons)
                        b.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
                    btn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 0.35f);
                }
            });
            _inventoryButtons.Add(btn);
        }
    }

    // ---------------- HELPERS ----------------

    private static bool InBounds(Vector2Int c, Vector2Int size)
        => c.x >= 0 && c.y >= 0 && c.x < size.x && c.y < size.y;

    private Button CreateCellButton(Transform parent, int x, int y)
    {
        var go = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0.12f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => OnCellClicked(x, y));
        return btn;
    }

    private void OnCellClicked(int x, int y)
    {
        if (_board == null) return;
        var cell = new Vector2Int(x, y);

        if (_removeToggle.isOn)
        {
            // 해당 셀에 걸친 피스를 찾아 제거
            _pieces.Clear();
            _board.GetPiecePlacements(_pieces);
            foreach (var p in _pieces)
            {
                foreach (var c in p.OccupiedCells)
                {
                    if (c == cell)
                    {
                        if (_binder.RemovePiece(p.Asset))
                        {
                            RefreshGrid();
                            RefreshInventory();
                        }
                        return;
                    }
                }
            }
        }
        else
        {
            // 선택된 인벤토리 아이템을 배치
            if (_selectedItem.HasValue)
            {
                if (_binder.TryPlaceInventoryPiece(_selectedItem.Value, cell, false))
                {
                    _selectedItem = null; // 사용 성공 시 선택 해제
                    RefreshGrid();
                    RefreshInventory();
                }
                else
                {
                    Debug.LogWarning($"[Overlay] Place failed at {cell}");
                }
            }
        }
    }

    // === Overlay 열고 닫기 ===

    public void Open(PlayerSystem.PlayerMemoryBinder binder)
    {
        _binder = binder;

        // UI가 아직 안 만들어졌다면(비활성 생성 등) 한 번 생성
        if (!_built)
        {
            EnsureEventSystem();
            BuildCanvas();
            BuildHeader();
            BuildGrid();
            BuildInventory();
            _built = true;
        }

        gameObject.SetActive(true);
        _open = true;

        // 새 바인더 이벤트 구독 (중복 방지 위해 먼저 한번 제거)
        _binder.BoardListChanged -= OnBoardListChanged;
        _binder.ActiveBoardChanged -= OnActiveBoardChanged;
        _binder.BoardChanged -= OnBoardChanged;
        _binder.InventoryChanged -= OnInventoryChanged;

        _binder.BoardListChanged += OnBoardListChanged;
        _binder.ActiveBoardChanged += OnActiveBoardChanged;
        _binder.BoardChanged += OnBoardChanged;
        _binder.InventoryChanged += OnInventoryChanged;

        // 안전한 초기 렌더 (null 가드)
        RefreshHeader();
        RebuildGrid();
        RefreshGrid();
        RefreshInventory();
    }

    public void Close()
    {
        if (!_open) return;
        _open = false;

        if (_binder != null)
        {
            _binder.BoardListChanged -= OnBoardListChanged;
            _binder.ActiveBoardChanged -= OnActiveBoardChanged;
            _binder.BoardChanged -= OnBoardChanged;
            _binder.InventoryChanged -= OnInventoryChanged;
        }

        if (_canvas) Destroy(_canvas.gameObject);
        Destroy(gameObject);
    }



    // ---------- UI Factory ----------



    private static GameObject CreatePanel(Transform parent, Color col)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = col;
        return go;
    }

    private static Image CreateImage(Transform parent, Color col)
    {
        var go = new GameObject("Image", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = col;
        return img;
    }

    private static Text CreateText(Transform parent, string text, int size, TextAnchor anchor, FontStyle style, Color col)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
        {
            // 실패 시 OS 폰트 폴백
            try { t.font = Font.CreateDynamicFontFromOSFont("Arial", size); }
            catch { /* 마지막 폴백: 아무 것도 없으면 그대로 진행 */ }
        }
        t.fontSize = size;
        t.alignment = anchor;
        t.fontStyle = style;
        t.color = col;
        return t;
    }

    private static Button CreateButton(Transform parent, string label)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0.12f);
        var btn = go.GetComponent<Button>();

        var txt = CreateText(go.transform, label, 16, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white);
        var tRt = txt.rectTransform;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = tRt.offsetMax = Vector2.zero;

        var layout = go.AddComponent<LayoutElement>();
        layout.minWidth = 100;
        layout.minHeight = 32;

        return btn;
    }

    private static Toggle CreateToggle(Transform parent, string label)
    {
        var go = new GameObject("Toggle", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rootRt = (RectTransform)go.transform;
        rootRt.sizeDelta = new Vector2(120, 28);

        // Background
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = new Vector2(0, 0.5f);
        bgRt.anchorMax = new Vector2(0, 0.5f);
        bgRt.sizeDelta = new Vector2(22, 22);
        bgRt.anchoredPosition = new Vector2(12, 0);
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.15f);

        // Checkmark
        var ck = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        ck.transform.SetParent(bg.transform, false);
        var ckRt = (RectTransform)ck.transform;
        ckRt.anchorMin = ckRt.anchorMax = new Vector2(0.5f, 0.5f);
        ckRt.sizeDelta = new Vector2(14, 14);
        ckRt.anchoredPosition = Vector2.zero;
        var ckImg = ck.GetComponent<Image>();
        ckImg.color = new Color(0.2f, 0.7f, 1f, 1f);

        // Label
        var txt = CreateText(go.transform, label, 14, TextAnchor.MiddleLeft, FontStyle.Normal, Color.white);
        var tRt = txt.rectTransform;
        tRt.anchorMin = new Vector2(0, 0);
        tRt.anchorMax = new Vector2(1, 1);
        tRt.offsetMin = new Vector2(40, 0);
        tRt.offsetMax = new Vector2(0, 0);

        // Toggle
        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = ckImg;
        toggle.isOn = false;

        return toggle;
    }

    private static Button CreateListButton(Transform parent, string label)
    {
        var btn = CreateButton(parent, label);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(0, 34);
        btn.GetComponent<Image>().color = new Color(1, 1, 1, 0.10f);
        return btn;
    }
}
