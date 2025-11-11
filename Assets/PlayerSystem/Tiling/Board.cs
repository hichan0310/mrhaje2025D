using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public abstract class Board : Trigger
    {
        public GameObject BackgroundUIObject { get; set; }
        protected List<(int x, int y, Polyomino p)> polyominos = new();

        // 프리뷰 상태
        [SerializeField] public Polyomino selected;
        private int previewX, previewY;
        private bool hasPreview;

        protected abstract Vector3 cellPos2Real(int x, int y);

        public IGetBoardItem getBoardItem { get; set; }

        public bool show { get; set; }


        private void Start()
        {
            BackgroundUIObject=this.gameObject;
        }


        public void click(int x, int y)
        {
            if (this.selected == null)
            {
                var poly = this.popPolyomino(x, y);
                if (poly != null)
                {
                    this.selected = poly.Value.p;
                    //Debug.Log(this.selected);
                    //Debug.Log(this.polyominos.Count);
                }
            }
            else
            {
                if (tryAddPolyomino(this.selected, x, y))
                    this.selected = null;
            }
        }


        // 기본 6x6
        protected virtual bool isOnBoard(int x, int y)
        {
            return 0 <= x && x < 6 && 0 <= y && y < 6;
        }

        // x, y 위치에 Polyomino의 (0,0) 피벗을 두고 배치 시도
        public bool tryAddPolyomino(Polyomino polyomino, int x, int y)
        {
            if (!polyomino || polyomino.cells == null || polyomino.cells.Count == 0) return false;

            // 경계 및 타겟 좌표 수집
            var targets = new List<(int gx, int gy)>(polyomino.cells.Count);
            foreach (var c in polyomino.cells)
            {
                int gx = x + c.x, gy = y + c.y;
                if (!isOnBoard(gx, gy)) return false;
                targets.Add((gx, gy));
            }

            // 기존 배치와 겹침 검사
            foreach (var kv in polyominos)
            {
                var pivot = (kv.x, kv.y);
                var other = kv.p;
                if (!other || other.cells == null) continue;

                foreach (var oc in other.cells)
                {
                    int ogx = pivot.x + oc.x, ogy = pivot.y + oc.y;
                    for (int i = 0; i < targets.Count; i++)
                        if (targets[i].gx == ogx && targets[i].gy == ogy)
                            return false;
                }
            }

            // 통과하면 등록하고 시각 배치
            polyominos.Add((x, y, polyomino));
            polyomino.display(x, y, cellPos2Real);
            return true;
        }

        // 전역 좌표에 놓인 폴리오미노 조회
        public (int x, int y, Polyomino p)? popPolyomino(int x, int y)
        {
            //Debug.Log(this.polyominos.Count);
            foreach (var kv in polyominos)
            {
                var pivot = (kv.x, kv.y);
                var p = kv.p;
                int lx = x - pivot.x, ly = y - pivot.y;
                if (p != null && p.isPosCell(lx, ly))
                {
                    //Debug.Log(p.cells.ToString());
                    polyominos.Remove(kv);
                    return (pivot.x, pivot.y, p);
                }
            }

            return null;
        }

        public void openBoard()
        {
            if (BackgroundUIObject) BackgroundUIObject.SetActive(true);
            foreach (var kv in polyominos)
            {
                var pivot = (kv.x, kv.y);
                var p = kv.p;
                if (p != null) p.display(pivot.x, pivot.y, cellPos2Real);
            }
        }

        public void closeBoard()
        {
            foreach (var kv in polyominos)
            {
                var p = kv.p;
                if (p != null) p.destroy(); // 비활성화
            }

            if (BackgroundUIObject) BackgroundUIObject.SetActive(false);
        }

        // 프리뷰 시작
        public void BeginPreview(Polyomino poly)
        {
            if (selected != null) selected.destroy();
            selected = poly;
            hasPreview = false;
        }

        // 스크린 마우스를 y=0 평면으로 사영
        protected virtual bool TryScreenToBoardPlane(Vector3 mouseScreen, out Vector3 hitWorld)
        {
            hitWorld = default;
            var cam = Camera.main;
            if (!cam) return false;

            var ray = cam.ScreenPointToRay(mouseScreen);
            var plane = new Plane(Vector3.up, 0f);
            if (plane.Raycast(ray, out float enter))
            {
                hitWorld = ray.GetPoint(enter);
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (show)
            {
                this.BackgroundUIObject.SetActive(true);
                foreach (var poly in polyominos) poly.p.gameObject.SetActive(true);
                if (Input.GetKeyDown(KeyCode.R)) this.selected?.rotate();
                if (this.selected == null) return;

                var mouse = Input.mousePosition;

                // 1) UI Canvas 모드
                var canvas = GetComponentInParent<Canvas>();
                var rt = this.selected.GetComponent<RectTransform>();
                if (rt != null && canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvas.transform as RectTransform,
                            mouse,
                            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                            out var local))
                    {
                        rt.anchoredPosition = local; // 마우스 그대로 따라감
                    }

                    return;
                }

                // 2) 월드 스페이스 모드
                if (TryScreenToBoardPlane(mouse, out var hitWorld))
                {
                    this.selected.transform.position = hitWorld; // 보드 평면 위로 따라감
                }
            }
            else
            {
                if (this.selected != null)
                {
                    this.getBoardItem.somethingSelected(this.selected);
                    this.selected = null;
                }
                foreach (var poly in polyominos) poly.p.Hide();
                this.BackgroundUIObject.SetActive(false);
            }
        }
    }
}