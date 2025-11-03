using System;
using TMPro;
using UnityEngine;

namespace PlayerSystem.Skills
{
    public class CooldownUI:MonoBehaviour
    {
        [SerializeField] private Skill skill;
        [SerializeField] private TMP_Text text;

        private void Update()
        {
            text.text = ((float)((int)(skill.timeleft*10))/10).ToString();
        }
    }
}