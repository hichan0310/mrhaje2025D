using System.Collections.Generic;
using EntitySystem;
using PlayerSystem.Weapons;
using UnityEngine;

namespace PlayerSystem
{
    public class AimSupport : MonoBehaviour
    {
        public static AimSupport Instance { get; protected set; }

        public Player player;
        public Weapon weapon;
        public LayerMask wallMask;

        public float assistRange = 1f;
        public float aimRange { get; protected set; }
        public Vector3 target { get; protected set; }
        public Transform targetTransform { get; protected set; }

        protected LineRenderer lr;
        protected LineRenderer lrLeft;
        protected LineRenderer lrRight;

        protected float aimStartTime;
        protected bool isAiming = false;

        protected const float MAX_AIM_RANGE = 10f;
        protected const float MIN_AIM_RANGE = 0.1f;
        protected const float AIM_DECAY_RATE = 7f; // 지수 감소 속도


        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            lr = CreateAimLineRenderer("MainAimLine");
            lr.material.color = Color.white;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;

            // 좌우 조준선용 LineRenderer 생성
            lrLeft = CreateAimLineRenderer("LeftAimLine");
            lrRight = CreateAimLineRenderer("RightAimLine");

            aimRange = MAX_AIM_RANGE;
        }

        LineRenderer CreateAimLineRenderer(string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            LineRenderer newLr = obj.AddComponent<LineRenderer>();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", Color.cyan);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0f, 1f, 1f, 1f) * 5f);
            newLr.material = mat;

            newLr.positionCount = 2;
            newLr.startWidth = 0.015f;
            newLr.endWidth = 0.005f;
            newLr.startColor = new Color(0f, 1f, 1f, 0.5f);
            newLr.endColor = new Color(0f, 1f, 1f, 0.5f);

            return newLr;
        }

        protected virtual void Update()
        {
            if(player==null) return;
            HandleAimInput();
            UpdateTargeting();
            UpdateAimLines();
        }

        protected virtual void HandleAimInput()
        {
            // 마우스 왼쪽 버튼을 누르고 있을 때
            if (Input.GetMouseButtonDown(0))
            {
                isAiming = true;
                aimStartTime = Time.time;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isAiming = false;
                aimRange = MAX_AIM_RANGE; // 리셋
            }

            // 조준 중일 때 aimRange 지수적 감소
            if (isAiming)
            {
                float aimDuration = Time.time - aimStartTime;
                // 지수 감소: aimRange = MIN + (MAX - MIN) * e^(-k*t)
                aimRange = MIN_AIM_RANGE + (MAX_AIM_RANGE - MIN_AIM_RANGE) * Mathf.Exp(-AIM_DECAY_RATE * aimDuration);
            }
        }

        protected virtual void UpdateTargeting()
        {
            Vector3 start = player.transform.position;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            Vector3 aimDir = (mouseWorld - start).normalized;
            Vector3 targetPos = mouseWorld; // 기본: 마우스

            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (EnemyLockOnPoint enemy in FindObjectsByType<EnemyLockOnPoint>(0))
            {
                Vector2 toEnemy = (enemy.transform.position - mouseWorld);
                float dist = toEnemy.magnitude;

                if (dist > assistRange) continue;

                if (dist < bestScore)
                {
                    bestScore = dist;
                    bestTarget = enemy.transform;
                }
            }

            if (bestTarget)
            {
                targetPos = bestTarget.position;
            }

            this.target = targetPos;
            this.targetTransform = bestTarget;

            // 메인 조준선 업데이트
            Vector2 e = targetPos;
            Vector2 s = start;

            lr.SetPosition(0, s);
            lr.SetPosition(1, e);
        }

        protected virtual void UpdateAimLines()
        {
            Vector3 start = player.transform.position;
            var tmp = (target - start);
            Vector3 direction = tmp.normalized;
            float length = tmp.magnitude;

            // 현재 방향의 각도 (2D)
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 좌우 각도 계산 (aimRange의 절반씩)
            float leftAngle = baseAngle + aimRange / 2f;
            float rightAngle = baseAngle - aimRange / 2f;

            // 좌측 조준선
            Vector3 leftDir = new Vector3(
                Mathf.Cos(leftAngle * Mathf.Deg2Rad),
                Mathf.Sin(leftAngle * Mathf.Deg2Rad),
                0f
            );
            Vector3 leftEnd = start + leftDir * (length * 0.95f);

            lrLeft.SetPosition(0, start);
            lrLeft.SetPosition(1, leftEnd);

            // 우측 조준선
            Vector3 rightDir = new Vector3(
                Mathf.Cos(rightAngle * Mathf.Deg2Rad),
                Mathf.Sin(rightAngle * Mathf.Deg2Rad),
                0f
            );
            Vector3 rightEnd = start + rightDir * (length * 0.95f);

            lrRight.SetPosition(0, start);
            lrRight.SetPosition(1, rightEnd);

            // 조준 중이 아닐 때는 좌우 조준선 숨기기
            lrLeft.enabled = isAiming;
            lrRight.enabled = isAiming;
        }
    }
}