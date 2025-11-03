using TMPro;
using UnityEngine;

namespace DefaultNamespace
{

    public class timer : MonoBehaviour
    {
        public TMP_Text timerText;
        public bool countDown = false;
        public float startSeconds = 0f;
        public bool showMilliseconds = false;

        float t;
        bool running = true;

        void OnEnable()
        {
            t = countDown ? startSeconds : 0f;
            running = true;
        }

        void Update()
        {
            if (!running) return;

            t += (countDown ? -1f : 1f) * Time.deltaTime;
            if (countDown && t <= 0f) { t = 0f; running = false; }

            int m = Mathf.FloorToInt(t / 60f);
            float s = t - m * 60f;
            timerText.text = showMilliseconds
                ? $"{m:00}:{s:00.00}"
                : $"{m:00}:{Mathf.FloorToInt(s):00}";
        }

        // 필요하면 버튼에 연결
        public void StartTimer() { running = true; }
        public void StopTimer()  { running = false; }
        public void ResetTimer() { t = countDown ? startSeconds : 0f; }
    }

}