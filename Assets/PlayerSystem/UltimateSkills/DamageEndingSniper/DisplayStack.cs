using System;
using EntitySystem;
using TMPro;
using UnityEngine;

namespace PlayerSystem.UltimateSkills.DamageEndingSniper
{
    public class DisplayStack : MonoBehaviour
    {
        public int stack { get; set; } = 0;
        // 내부쿨 넣어야 하는데 귀찮다
        public Entity target { get; set; }
        private TMP_Text text;

        private void Start()
        {
            this.text = gameObject.GetComponent<TMP_Text>();
            this.transform.parent = target.hpBar.transform;
            this.transform.localPosition = Vector3.zero;
        }

        private void Update()
        {
            text.text = stack.ToString();
            if(stack>100) stack = 100;
        }
    }
}