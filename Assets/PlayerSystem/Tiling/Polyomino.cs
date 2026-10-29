using System;
using System.Collections.Generic;
using EntitySystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlayerSystem.Tiling
{
    // 이 컴포넌트가 붙은 GameObject 하나가 폴리오미노 전체 모양의 렌더러다.
    // 모델의 피벗이 셀 (0,0) 중심에 오도록 제작돼 있다고 가정한다.
    public abstract class Polyomino : MonoBehaviour, ITriggerEffect
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        [Tooltip("피벗(0,0)을 기준으로 차지하는 셀 좌표들")] public List<Cell> cells;

        public RectTransform rt { get; private set; }


        protected virtual void Awake()
        {
            this.rt = GetComponent<RectTransform>();

            var cg = GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

            cg.blocksRaycasts = false;
        }

        private bool equipmentMode;
        public void UseEquipmentMode()
        {
            equipmentMode = true;
            foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.enabled = false;
            var group = GetComponent<CanvasGroup>();
            if (group) { group.alpha = 0; group.blocksRaycasts = false; group.interactable = false; }
        }

        protected virtual void Start()
        {
            // Debug.Log("fuck");
            if (!equipmentMode) setStatBonus();
            // Debug.Log(this.statBonusDescription);
        }

        public void OnReturnToBag(Transform bagParent)
        {
            // 부모 복구
            transform.SetParent(bagParent, false);

            // UI 기준 초기화
            rt.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            gameObject.SetActive(true);
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

        public abstract void trigger(Entity player, float power);

        public void register(Player player)
        {
            for (int i = 0; i < statBonus.Length; i++)
            {
                statBonus[i](player, true, statBonusId[i] * bonusAmount[i]);
            }
        }

        public void unregister(Player player)
        {
            for (int i = 0; i < statBonus.Length; i++)
            {
                statBonus[i](player, false, statBonusId[i] * bonusAmount[i]);
            }
        }

        protected int[] statBonusId = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        protected int[] bonusAmount = new[] { 1, 1, 1, 1, 1, 1, 200, 100, 100 };

        protected string[] statBonusName = new[]
        {
            "발사 속도",
            "총알 개수",
            "총알 속도",
            "추가 지속시간",
            "스킬 범위",
            "쿨다운 감소",
            "최대체력",
            "기초공력력",
            "기초방어력",
        };

        protected string statBonusDescription = "";

        private Action<Player, bool, int>[] statBonus = new[]
        {
            (Action<Player, bool, int>)((player, up, a) => player.stat.fireSpeed += up ? a : -a),
            (player, up, a) => player.stat.bulletRate += up ? a : -a,
            (player, up, a) => player.stat.bulletSpeed += up ? a : -a,
            (player, up, a) => player.stat.additionalDuration += up ? a : -a,
            (player, up, a) => player.stat.skillRange += up ? a : -a,
            (player, up, a) => player.stat.skillCooldownDecrease += up ? a : -a,
            (player, up, a) => player.stat.maxHp += up ? a : -a,
            (player, up, a) => player.stat.baseAtk += up ? a : -a,
            (player, up, a) => player.stat.baseDef += up ? a : -a,
        };

        private void setStatBonus()
        {
            int n = this.cells.Count;
            int bonusNum = 0;
            switch (n)
            {
                case 1:
                case 2:
                    break;
                case 3:
                case 4:
                    bonusNum = 1;
                    break;
                case 5:
                case 6:
                    bonusNum = 2;
                    break;
                case 7:
                case 8:
                    bonusNum = 3;
                    break;
                case 9:
                case 10:
                    bonusNum = 4;
                    break;
                case 11:
                case 12:
                    bonusNum = 5;
                    break;
                default:
                    break;
            }

            for (int i = 0; i < bonusNum; i++)
                this.statBonusId[Random.Range(0, statBonus.Length)] += 1;

            this.statBonusDescription = "";
            for (int i = 0; i < statBonus.Length; i++)
            {
                if (this.statBonusId[i] != 0)
                    this.statBonusDescription += $"{statBonusName[i]} +{statBonusId[i] * bonusAmount[i]}\n";
            }
        }
    }
}