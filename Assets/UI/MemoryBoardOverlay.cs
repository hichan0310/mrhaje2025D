// self-building lightweight editor overlay for Memory Board
// Drop-in ready: if any UI refs are missing, it will auto-build them at runtime.

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PlayerSystem;

public class MemoryBoardOverlay : MonoBehaviour
{
    [Header("UI (optional: will be auto-built if left empty)")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform tabsRoot;               // (옵션)
    [SerializeField] private Button tabButtonPrefab;               // (옵션)
    [SerializeField] private RectTransform gridRoot;               // GridLayoutGroup 필요(자동 생성됨)
    [SerializeField] private Image cellPrefab;                     // 없으면 런타임에 기본 셀 프리팹 생성
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Dropdown paletteDropdown;         // (옵션)
    [SerializeField] private Button rotateLeftBtn;                 // (옵션)
    [SerializeField] private Button rotateRightBtn;                // (옵션)
    [SerializeField] private TMP_Text stateLabel;

    [Header("Inventory UI (optional: will be auto-built if left empty)")]
    [SerializeField] private ScrollRect inventoryScroll;
    [SerializeField] private RectTransform inventoryContent;
    [SerializeField] private Button inventoryItemButtonPrefab;     // 없으면 런타임에 기본 버튼 프리팹 생성

    // ─────────────────────────────────────────────────────────────────────────────

    private PlayerMemoryBinder binder;
    private Vector2Int boardSize;
    private readonly Dictionary<Vector2Int, Image> cellMap = new();
    private readonly List<PlayerMemoryBinder.PlacementInfo> tmpPlacements = new();

    // 선택된 인벤토리 아이템
    private MemoryPieceAsset selectedAsset;
    private float selectedMultiplier = 1f;

    // 인벤토리 View 캐싱
    private readonly List<Button> invButtons = new();

    // ─────────────────────────────────────────────────────────────────────────────
    // ⭐️ 편의 헬퍼: 어디서든 한 줄로 열기
    public static MemoryBoardOverlay ShowForPlayer(Player player)
    {
        if (!player) return null;
        var binder = player.GetComponent<PlayerMemoryBinder>();
        if (!binder) { Debug.LogWarning("[Overlay] PlayerMemoryBinder not found on Player."); return null; }

        var overlay = EnsureSingleton();
        overlay.Open(binder);
        return overlay;
    }

    public static MemoryBoardOverlay EnsureSingleton()
    {
        // 비활성 포함 탐색
#if UNITY_2020_1_OR_NEWER
        var overlay = FindObjectOfType<MemoryBoardOverlay>(true);
#else
        var all = Resources.FindObjectsOfTypeAll<MemoryBoardOverlay>();
        var overlay = (all != null && all.Length > 0) ? all[0] : null;
#endif
        if (overlay) return overlay;

        // 없으면 생성
        var go = new GameObject("MemoryBoardOverlay(Auto)");
        overlay = go.AddComponent<MemoryBoardOverlay>();
        overlay.BuildIfNeeded();
        go.SetActive(false); // Open()에서 보여줌
        return overlay;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    public void Open(PlayerMemoryBinder b)
    {
        BuildIfNeeded(); // 누락된 UI 있으면 자동 생성
        gameObject.SetActive(true);

        binder = b;
        if (!binder)
        {
            Debug.LogWarning("[Overlay] binder is null");
            return;
        }

        // 보드 크기
        boardSize = binder.BoardSize;

        // 이벤트 구독
        binder.InventoryChanged += OnInventoryChanged;
        binder.BoardChanged += OnBoardChanged;

        BuildGrid();
        RefreshBoard();
        RefreshInventory();

        Show();
    }

    public void Close()
    {
        if (binder != null)
        {
            binder.InventoryChanged -= OnInventoryChanged;
            binder.BoardChanged -= OnBoardChanged;
        }
        Hide();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // 간단한 닫기 단축키
        if (canvasGroup && canvasGroup.interactable && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void OnDestroy() => Close();

    private void OnInventoryChanged() => RefreshInventory();
    private void OnBoardChanged() => RefreshBoard();

    // ─────────────────────────────────────────────────────────────────────────────
    private void Show()
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MemoryBoardOverlay.cs 의 BuildGrid() 교체/보강
    private void BuildGrid()
    {
        if (binder == null)
            return;

        // 1) boardSize 보정 (0이면 안전 기본값)
        boardSize = binder.BoardSize;
        if (boardSize.x < 1 || boardSize.y < 1)
            boardSize = new Vector2Int(8, 6);

        // 2) 기존 셀 정리
        foreach (var kv in cellMap)
            if (kv.Value) Destroy(kv.Value.gameObject);
        cellMap.Clear();

        // 3) GridLayoutGroup 확보(+없으면 추가)
        var grid = gridRoot ? gridRoot.GetComponent<GridLayoutGroup>() : null;
        if (!gridRoot)
        {
            // gridRoot가 비어있으면 즉석 생성
            var g = new GameObject("GridRoot", typeof(RectTransform), typeof(GridLayoutGroup));
            g.transform.SetParent(transform, false);
            gridRoot = g.GetComponent<RectTransform>();
        }
        if (!grid)
            grid = gridRoot.gameObject.GetComponent<GridLayoutGroup>() ?? gridRoot.gameObject.AddComponent<GridLayoutGroup>();

        // 4) gridRoot 기본 레이아웃 보정(너무 작으면 화면에 보이게)
        var rt = gridRoot;
        // 부모 안에서 넓게 차도록 (필요시 조정)
        if (rt.anchorMin == rt.anchorMax) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; }
        if (Mathf.Abs(rt.rect.width) < 10f || Mathf.Abs(rt.rect.height) < 10f)
        {
            // 상하/좌우 여백(픽셀) 강제
            rt.offsetMin = new Vector2(180, 200);
            rt.offsetMax = new Vector2(0, -240);
        }

        // 5) Grid 셋업
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, boardSize.x);
        grid.spacing = new Vector2(6, 6);

        // 그리드 폭이 아직 계산 안 됐으면 대략값 사용
        float gridWidth = rt.rect.width;
        if (gridWidth < 10f) gridWidth = 900f;
        float cellW = (gridWidth - grid.spacing.x * (boardSize.x - 1)) / Mathf.Max(1, boardSize.x);
        cellW = Mathf.Clamp(cellW, 40f, 96f);
        grid.cellSize = new Vector2(cellW, cellW);

        // 6) 셀 템플릿 보정 (없으면 기본 생성)
        var template = cellPrefab ? cellPrefab : CreateDefaultCellPrefab();

        // 7) 셀 생성 (반드시 활성화)
        for (int y = boardSize.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < boardSize.x; x++)
            {
                var cell = Instantiate(template, gridRoot);
                cell.gameObject.name = $"Cell({x},{y})";
                cell.gameObject.SetActive(true); // ★ 중요: 템플릿이 꺼져도 강제 활성화

                var pos = new Vector2Int(x, y);

                // 클릭 가능 보장
                var btn = cell.GetComponent<Button>();
                if (!btn) btn = cell.gameObject.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnCellClicked(pos));

                cellMap[pos] = cell;
                PaintCell(pos, empty: true);
            }
        }

        if (title) title.text = "Memory Board";
        if (stateLabel) stateLabel.text = "Select from inventory, then click a cell to place.\nClick a placed cell to remove.";
    }


    private void RefreshBoard()
    {
        if (binder == null) return;
        foreach (var kv in cellMap) PaintCell(kv.Key, empty: true);

        tmpPlacements.Clear();
        binder.GetPlacements(tmpPlacements);

        foreach (var p in tmpPlacements)
            foreach (var c in p.Occupied)
                if (cellMap.TryGetValue(c, out var img))
                    PaintCell(c, empty: false);
    }

    private void RefreshInventory()
    {
        if (!binder) return;
        EnsureInventoryBuilt();

        // 기존 버튼 삭제
        foreach (var b in invButtons)
            if (b) Destroy(b.gameObject);
        invButtons.Clear();

        // 같은 Asset+Multiplier 묶어서 수량 표시
        var grouped = binder.Inventory
            .Where(i => i.Asset)
            .GroupBy(i => new { i.Asset, Mul = Mathf.RoundToInt(i.PowerMultiplier * 1000f) })
            .Select(g => new
            {
                Asset = g.First().Asset,
                Mult = g.First().PowerMultiplier,
                Count = g.Count()
            })
            .OrderBy(e => e.Asset.DisplayName)
            .ThenBy(e => e.Mult)
            .ToList();

        foreach (var entry in grouped)
        {
            var btn = Instantiate(inventoryItemButtonPrefab, inventoryContent);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (!label)
            {
                // TMP가 없다면 일반 Text 찾기
                var legacyLabel = btn.GetComponentInChildren<Text>();
                if (legacyLabel)
                    legacyLabel.text = MakeInvLabel(entry.Asset, entry.Mult, entry.Count);
            }
            else
            {
                label.text = MakeInvLabel(entry.Asset, entry.Mult, entry.Count);
            }

            btn.onClick.AddListener(() =>
            {
                selectedAsset = entry.Asset;
                selectedMultiplier = entry.Mult;
                if (stateLabel)
                {
                    var mulTxt = Mathf.Approximately(selectedMultiplier, 1f) ? "" : $" ×{selectedMultiplier:0.##}";
                    stateLabel.text = $"Selected: {selectedAsset.DisplayName}{mulTxt}\nClick a cell to place.";
                }
            });

            invButtons.Add(btn);
        }

        // 스크롤 지정
        inventoryScroll.content = inventoryContent;
    }

    private string MakeInvLabel(MemoryPieceAsset asset, float mult, int count)
    {
        var mulTxt = Mathf.Approximately(mult, 1f) ? "" : $" ×{mult:0.##}";
        return $"{asset.DisplayName}{mulTxt}  (x{count})";
    }

    private void OnCellClicked(Vector2Int pos)
    {
        if (binder == null) return;

        // 이미 배치가 있으면 제거
        if (binder.RemovePlacementAt(pos))
        {
            RefreshBoard();
            return;
        }

        // 선택된 인벤토리 없으면 무시
        if (!selectedAsset) return;

        // 배치 시도
        if (binder.TryPlaceFromInventory(selectedAsset, selectedMultiplier, pos, out var placed))
        {
            RefreshBoard();

            // 한 개밖에 없었으면 선택 해제
            var stillHas = binder.Inventory.Any(i => i.Asset == selectedAsset);
            if (!stillHas)
            {
                selectedAsset = null;
                selectedMultiplier = 1f;
                if (stateLabel) stateLabel.text = "Select from inventory, then click a cell to place.";
            }
        }
    }

    private void PaintCell(Vector2Int pos, bool empty)
    {
        if (!cellMap.TryGetValue(pos, out var img) || !img) return;
        img.color = empty ? new Color(0.16f, 0.16f, 0.16f, 0.9f) : new Color(0.36f, 0.68f, 0.94f, 0.9f);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 🔧 자동 빌더 (필수 구성요소가 비어 있으면 만들어줌)
    private void BuildIfNeeded()
    {
        // EventSystem 보장
        EnsureEventSystem();

        // Canvas/CanvasGroup 보장
        var root = transform as RectTransform;
        var canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.layer = gameObject.layer;
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cgo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            transform.SetParent(cgo.transform, false);
        }

        if (!canvasGroup)
            canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // 루트 리지드 레이아웃
        var rootRT = transform as RectTransform;
        if (rootRT)
        {
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;
        }

        // 상단 헤더 만들기 (타이틀/드롭다운/상태라벨/회전버튼)
        BuildHeaderIfMissing(canvas.transform);

        // 좌측 탭(옵션)
        if (!tabsRoot)
        {
            var tabs = new GameObject("TabsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            tabs.transform.SetParent(transform, false);
            var rt = tabs.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(160, 0);
            tabsRoot = rt;

            var v = tabs.GetComponent<VerticalLayoutGroup>();
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            v.spacing = 8f; v.childAlignment = TextAnchor.UpperCenter;

            var fit = tabs.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // 중앙 보드 그리드
        if (!gridRoot)
        {
            var g = new GameObject("GridRoot", typeof(RectTransform), typeof(GridLayoutGroup));
            g.transform.SetParent(transform, false);
            var rt = g.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(180, 120);   // 좌측 탭/하단 인벤토리 여백
            rt.offsetMax = new Vector2(0, -240);
            gridRoot = rt;

            var grid = g.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.spacing = new Vector2(6, 6);
            grid.cellSize = new Vector2(64, 64);
        }

        // 하단 인벤토리 스크롤뷰
        EnsureInventoryBuilt();

        // 기본 셀 프리팹
        if (!cellPrefab)
            cellPrefab = CreateDefaultCellPrefab();

        // 기본 인벤토리 버튼 프리팹
        if (!inventoryItemButtonPrefab)
            inventoryItemButtonPrefab = CreateDefaultInventoryButtonPrefab();
    }

    private void BuildHeaderIfMissing(Transform canvas)
    {
        if (title && paletteDropdown && stateLabel && rotateLeftBtn && rotateRightBtn)
            return;

        var header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(transform, false);
        var rt = header.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 100);

        // Title
        if (!title)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(header.transform, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = 30; t.alignment = TextAlignmentOptions.MidlineLeft;
            t.text = "Memory Board";
            var tr = go.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(0.5f, 1);
            tr.offsetMin = new Vector2(20, 0);
            tr.offsetMax = new Vector2(-10, 0);
            title = t;
        }

        // State label
        if (!stateLabel)
        {
            var go = new GameObject("StateLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(header.transform, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = 20; t.alignment = TextAlignmentOptions.MidlineRight;
            t.text = "Select from inventory, then click a cell to place.";
            var tr = go.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 0);
            tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = new Vector2(10, 0);
            tr.offsetMax = new Vector2(-20, 0);
            stateLabel = t;
        }

        // 간단한 회전 버튼/팔레트는 생략 가능(프로그래머블)
    }

    private void EnsureInventoryBuilt()
    {
        if (inventoryScroll && inventoryContent) return;

        // ScrollView
        var scrollGO = new GameObject("InventoryScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
        scrollGO.transform.SetParent(transform, false);
        var srt = scrollGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0);
        srt.anchorMax = new Vector2(1, 0);
        srt.pivot = new Vector2(0.5f, 0);
        srt.sizeDelta = new Vector2(0, 200);
        srt.anchoredPosition = Vector2.zero;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGO.transform, false);
        var vrt = viewport.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0, 0);
        vrt.anchorMax = new Vector2(1, 1);
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

        var v = content.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true; v.childControlWidth = true;
        v.childForceExpandHeight = false; v.childForceExpandWidth = true;
        v.spacing = 8f;

        var fit = content.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.content = crt;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // 기본 색/마스크
        var bg = scrollGO.GetComponent<Image>(); bg.color = new Color(0, 0, 0, 0.35f);
        var vpImg = viewport.GetComponent<Image>(); vpImg.color = new Color(0, 0, 0, 0.25f);
        var vpMask = viewport.GetComponent<Mask>(); vpMask.showMaskGraphic = false;

        inventoryScroll = scroll;
        inventoryContent = crt;
    }

    private Image CreateDefaultCellPrefab()
    {
        var go = new GameObject("CellPrefab(Image+Button)", typeof(RectTransform), typeof(Image), typeof(Button));
        var img = go.GetComponent<Image>();
        img.color = new Color(0.16f, 0.16f, 0.16f, 0.9f);
        // 프리팹처럼 쓸 임시 오브젝트 (씬에 보이면 거슬리니 비활성)
        go.SetActive(false);
        return img;
    }

    private Button CreateDefaultInventoryButtonPrefab()
    {
        var go = new GameObject("InventoryItemButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var img = go.GetComponent<Image>(); img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        var t = labelGO.GetComponent<TextMeshProUGUI>();
        t.fontSize = 24;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.text = "Item";
        var rt = labelGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(16, 8); rt.offsetMax = new Vector2(-16, -8);

        go.SetActive(false);
        return go.GetComponent<Button>();
    }

    private static void EnsureEventSystem()
    {
#if UNITY_2020_1_OR_NEWER
        var es = FindObjectOfType<EventSystem>(true);
#else
        var all = Resources.FindObjectsOfTypeAll<EventSystem>();
        var es = (all != null && all.Length > 0) ? all[0] : null;
#endif
        if (es) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
    }
}
