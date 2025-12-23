using System;
using TMPro;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public class InventoryBoardText: MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Inventory inventory;
        [SerializeField] private int index;

        private void Update()
        {
            this.text.text=inventory.boards[index].Name;
        }
    }
}