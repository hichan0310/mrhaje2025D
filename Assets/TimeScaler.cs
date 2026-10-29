using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class TimeScaler : MonoBehaviour
    {
        public static TimeScaler Instance;
        private float baseFixed;
        private float baseScale;
        private readonly Dictionary<object, float> factors = new();

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(this); return; }
            Instance = this;
            baseFixed = Time.fixedDeltaTime;
            baseScale = Time.timeScale;
        }

        private void Update() => Time.fixedDeltaTime = Mathf.Max(0.0001f, baseFixed * Time.timeScale);

        // These existing APIs change the baseline; temporary/cinematic factors remain independent.
        public void SetTimeScale(float scale)
        {
            baseScale = Mathf.Clamp(scale, 0, 100);
            Apply();
        }

        public void changeTimeScale(float scale)
        {
            if (factors.Count == 0) baseScale = Time.timeScale;
            baseScale = Mathf.Clamp(baseScale * Mathf.Max(0, scale), 0, 100);
            Apply();
        }

        public void SetFactor(object owner, float scale)
        {
            if (owner == null) return;
            if (factors.Count == 0) baseScale = Time.timeScale;
            factors[owner] = Mathf.Max(0, scale);
            Apply();
        }

        public void RemoveFactor(object owner)
        {
            if (owner != null && factors.Remove(owner)) Apply();
        }

        public void changeScaleForRealTime(float scale, float seconds)
        {
            var token = new object();
            SetFactor(token, scale);
            StartCoroutine(Expire(token, seconds));
        }

        private IEnumerator Expire(object token, float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0, seconds));
            RemoveFactor(token);
        }

        private void Apply()
        {
            float value = baseScale;
            foreach (float factor in factors.Values) value *= factor;
            Time.timeScale = Mathf.Clamp(value, 0, 100);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (Instance != this) return;
            factors.Clear();
            Time.timeScale = baseScale;
            Time.fixedDeltaTime = baseFixed;
            Instance = null;
        }

        private void OnEnable()
        {
            if (!Instance) { Instance = this; baseScale = Time.timeScale; }
        }
    }
}
