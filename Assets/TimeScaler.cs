using System;
using System.Collections;
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

        private void Update()
        {
            Time.fixedDeltaTime = baseFixed*Time.timeScale;
        }

        public void SetTimeScale(float s)   // 정말 이거 말고는 방법이 없을 경우에만 사용하자
        {
            Time.timeScale = s;
        }

        public void changeTimeScale(float scale)
        {
            Time.timeScale *= scale;
        }
        

        public void changeScaleForRealTime(float scale, float time)
        {
            Time.timeScale *= scale;
            StartCoroutine(CoDelay(time, () =>
            {
                Time.timeScale /= scale;
            }));
        }
        
        private IEnumerator CoDelay(float time, Action action)
        {
            yield return new WaitForSecondsRealtime(time);
            action?.Invoke();
        }
    }
}