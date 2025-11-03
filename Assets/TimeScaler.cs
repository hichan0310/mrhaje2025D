using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class TimeScaler : MonoBehaviour
    {
        public static TimeScaler Instance;
        
        float baseFixed;

        void Awake() => baseFixed = Time.fixedDeltaTime;

        private void Start()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);
        }

        public void SetTimeScale(float s)
        {
            Time.timeScale = s;
            Time.fixedDeltaTime = baseFixed * s; // 물리 프레임 유지
        }
    }
}