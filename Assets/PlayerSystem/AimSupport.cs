using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem
{
    public class AimSupport:MonoBehaviour
    {
        public static AimSupport Instance { get; private set; }
        public List<GameObject> moreLockOn = new();
        public Player player;
        private float assistAngleRange = 20f; // ±도
        public LayerMask wallMask;
        public Vector3 target { get; private set; }
        public Transform targetTransform { get; private set; }

        private LineRenderer lr;

        void Start()
        {
            if (Instance == null)
            {
                Instance = this; // Singleton 초기화
            }
            else
            {
                Destroy(gameObject); // 중복된 매니저 제거
            }
            lr = GetComponent<LineRenderer>();
            
            // lr.material = new Material(Shader.Find("Sprites/Default"));
            
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", Color.white); // 기본 색상
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 1f, 0f, 1f) * 10f); // HDR Emission (노란빛 + 세기)
            lr.material = mat;
            
            
            lr.positionCount = 2;
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.startColor = new Color(1f, 1f, 1f, 1f);
            lr.endColor = new Color(1f, 1f, 1f, 1f);
        }

        void Update()
        {
            Vector3 start = player.transform.position;
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            Vector3 aimDir = (mouseWorld - start).normalized;
            Vector3 targetPos = mouseWorld; // 기본: 마우스

            Transform bestTarget = null;
            float bestScore = float.MaxValue;
                
            foreach (GameObject enemy in moreLockOn)
            {
                if(!enemy) continue;
                if((mouseWorld-enemy.transform.position).magnitude>3) continue;
                
                Vector3 toEnemy = (enemy.transform.position - start);
                float dist = toEnemy.magnitude;

                float angle = Vector3.Angle(aimDir, toEnemy.normalized);
                if (angle > assistAngleRange) continue;

                // 벽 체크
                if (Physics2D.Raycast(start, toEnemy.normalized, dist, wallMask)) continue;
                
                if (dist < bestScore)
                {
                    bestScore = dist;
                    bestTarget = enemy.transform;
                }
            }
            
            moreLockOn.RemoveAll(item => !item);

            if (bestTarget)
            {
                targetPos = bestTarget.position;
            }

            lr.SetPosition(0, start);
            lr.SetPosition(1, targetPos);
            this.target = targetPos;
            this.targetTransform = bestTarget;
        }
    }
}