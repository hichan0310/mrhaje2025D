using TMPro;
using UnityEngine;

public class WorldTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    public void Set(string title, string desc)
    {
        nameText.text = title;
        descText.text = desc;
    }
}
