using TMPro;
using UnityEngine;

namespace PlayerSystem.UltimateSkills
{
    public class EnergyChargeUI:MonoBehaviour
    {
        [SerializeField] private Ultimate skill;
        [SerializeField] private TMP_Text text;

        private void Update()
        {
            text.text = skill.nowEnergy.ToString()+"/"+skill.energyCost.ToString();
        }
    }
}