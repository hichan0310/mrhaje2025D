using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public abstract class Board : MonoBehaviour
    {
        public GameObject BackgroundUIObject;
        protected Dictionary<(int x, int y), Polyomino> polyominos = new();

        // 프리뷰 상태
        [SerializeField] private Polyomino selected;
        [SerializeField] private int searchRadius = 8;
        [SerializeField] private bool limitPreviewToBoard = true;
        private int previewX, previewY;
        private bool hasPreview;

        protected abstract Vector3 cellPos2Real(int x, int y);

        // 기본 6x6
        protected virtual bool isOnBoard(int x, int y)
        {
            return 0 <= x && x < 6 && 0 <= y && y < 6;
        }

        protected virtual Cell? GetCellByPos(int mouseX, int mouseY)
        {
            return null;
        }

        // x, y 위치에 Polyomino의 (0,0) 피벗을 두고 배치 시도
        public void tryAddPolyomino(Polyomino polyomino, int x, int y)
        {
            if (polyomino == null || polyomino.cells == null || polyomino.cells.Count == 0) return;
            if (polyominos.ContainsKey((x, y))) return;

            // 경계 및 타겟 좌표 수집
            var targets = new List<(int gx, int gy)>(polyomino.cells.Count);
            foreach (var c in polyomino.cells)
            {
                int gx = x + c.x, gy = y + c.y;
                if (!isOnBoard(gx, gy)) return;
                targets.Add((gx, gy));
            }

            // 기존 배치와 겹침 검사
            foreach (var kv in polyominos)
            {
                var pivot = kv.Key;
                var other = kv.Value;
                if (other == null || other.cells == null) continue;

                foreach (var oc in other.cells)
                {
                    int ogx = pivot.x + oc.x, ogy = pivot.y + oc.y;
                    for (int i = 0; i < targets.Count; i++)
                        if (targets[i].gx == ogx && targets[i].gy == ogy) return;
                }
            }

            // 통과하면 등록하고 시각 배치
            polyominos[(x, y)] = polyomino;
            polyomino.display(x, y, cellPos2Real);
        }

        // 전역 좌표에 놓인 폴리오미노 조회
        public (int x, int y, Polyomino p)? getPolyomino(int x, int y)
        {
            foreach (var kv in polyominos)
            {
                var pivot = kv.Key;
                var p = kv.Value;
                int lx = x - pivot.x, ly = y - pivot.y;
                if (p != null && p.isPosCell(lx, ly))
                    return (pivot.x, pivot.y, p);
            }
            return null;
        }

        public void openBoard()
        {
            if (BackgroundUIObject) BackgroundUIObject.SetActive(true);
            foreach (var kv in polyominos)
            {
                var pivot = kv.Key; var p = kv.Value;
                if (p != null) p.display(pivot.x, pivot.y, cellPos2Real);
            }
        }

        public void closeBoard()
        {
            foreach (var kv in polyominos)
            {
                var p = kv.Value;
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

        // 가장 가까운 격자 좌표 찾기
        private bool TryFindSnapCell(Vector3 world, out int gx, out int gy)
        {
            float best = float.MaxValue; int bx = 0, by = 0; bool found = false;
            for (int x = -searchRadius; x <= searchRadius; x++)
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                if (limitPreviewToBoard && !isOnBoard(x, y)) continue;
                float d = (cellPos2Real(x, y) - world).sqrMagnitude;
                if (d < best) { best = d; bx = x; by = y; found = true; }
            }
            gx = bx; gy = by; return found;
        }

        private void Update()
        {
            if (selected == null) return;

            if (TryScreenToBoardPlane(Input.mousePosition, out var mouseWorld)
                && TryFindSnapCell(mouseWorld, out previewX, out previewY))
            {
                selected.display(previewX, previewY, cellPos2Real);
                hasPreview = true;
            }

            if (hasPreview && Input.GetKeyDown(KeyCode.R))
            {
                selected.rotate();
                selected.display(previewX, previewY, cellPos2Real);
            }

            if (hasPreview && Input.GetMouseButtonDown(0))
            {
                tryAddPolyomino(selected, previewX, previewY);
                selected = null;
                hasPreview = false;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (selected != null) selected.destroy();
                selected = null; hasPreview = false;
            }
        }
    }
}
