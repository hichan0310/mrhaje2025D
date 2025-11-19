using System;
using UnityEngine;

namespace Gameplay
{
    public class FollowTargetAndMouse : MonoBehaviour
    {
        public GameObject target;

        // 부드럽게 따라가는 정도 (값이 클수록 빠르게 따라감)
        public float smoothSpeed = 10f;
        public Vector2 offset;

        private void Update()
        {
            var z = transform.position.z;

            // 목표 위치 계산 (원래 로직 그대로)
            var desiredPos =new Vector3(offset.x, offset.y, 0)+
                (target.transform.position * 2f + Camera.main.ScreenToWorldPoint(Input.mousePosition)) / 3f;
            desiredPos.z = z;

            // 이전 좌표(transform.position)를 기준으로 부드럽게 보간
            transform.position = Vector3.Lerp(
                transform.position,   // 현재 위치(이전 좌표)
                desiredPos,           // 목표 위치
                Time.deltaTime * smoothSpeed
            );
        }
    }
}