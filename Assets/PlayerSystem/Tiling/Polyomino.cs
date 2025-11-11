using System;
using System.Collections.Generic;
using EntitySystem;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    // 이 컴포넌트가 붙은 GameObject 하나가 폴리오미노 전체 모양의 렌더러다.
    // 모델의 피벗이 셀 (0,0) 중심에 오도록 제작돼 있다고 가정한다.
    public abstract class Polyomino : MonoBehaviour, ITriggerEffect
    {
        [Tooltip("피벗(0,0)을 기준으로 차지하는 셀 좌표들")] public List<Cell> cells;

        public RectTransform rt { get; private set; }


        private void Start()
        {
            this.rt = this.GetComponent<RectTransform>();
        }

        public void Hide()
        {
            // Debug.Log("Asdfasdfasdf");
            this.gameObject.SetActive(false);
        }


        public (int, int) pivot => (0, 0);

        // 폴리오미노 전체 오브젝트 하나만 이동
        public void display(int pivotX, int pivotY, Func<int, int, Vector3> pos2real)
        {
            if (cells == null || cells.Count == 0) return;

            // 피벗 셀의 월드 위치에 배치 후 오프셋 적용
            var pivotWorld = pos2real(pivotX, pivotY);
            //if (!gameObject.activeSelf) gameObject.SetActive(true);
            //Debug.Log(pivotWorld);
            // transform.position = pivotWorld;
            this.rt.anchoredPosition = pivotWorld;
        }

        // 보드 닫기나 프리뷰 취소 시 비활성화
        public void destroy()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        // 로컬 좌표 기준 점유 확인
        public bool isPosCell(int x, int y)
        {
            if (cells == null) return false;
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].x == x && cells[i].y == y)
                    return true;
            return false;
        }

        // 논리 셀 회전 + 실제 모델 회전
        // 피벗(0,0) 기준 시계 90도
        public void rotate()
        {
            if (cells == null || cells.Count == 0) return;

            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                cells[i] = new Cell { x = -c.y, y = c.x };
            }

            // 90도 회전
            // UI라면 RectTransform 기준 회전
            var z = rt.localEulerAngles.z + 90f;
            z = Mathf.Repeat(z, 360f);
            rt.localEulerAngles = new Vector3(0f, 0f, z);
        }

        // 프리뷰용 스냅 이동
        public void followMouse(Func<int, int, Vector3> pos2real, Vector3 mousePos)
        {
            const int R = 8;
            int bestX = 0, bestY = 0;
            float best = float.MaxValue;

            for (int gx = -R; gx <= R; gx++)
            for (int gy = -R; gy <= R; gy++)
            {
                float d = (pos2real(gx, gy) - mousePos).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestX = gx;
                    bestY = gy;
                }
            }

            display(bestX, bestY, pos2real);
        }

        public abstract void trigger(Entity entity, float power);
    }
}