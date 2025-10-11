using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayerSystem;

namespace UI
{
    public class MemoryPieceInventoryItemView : MonoBehaviour
    {
        [SerializeField] private Button selectButton = null;
        [SerializeField] private Image iconImage = null;
        [SerializeField] private TMP_Text nameLabel = null;
        [SerializeField] private TMP_Text countLabel = null;
        [SerializeField] private GameObject selectionHighlight = null;

        private MemoryPieceAsset boundAsset = null;
        private float boundMultiplier = 1f;
        private Action<MemoryPieceInventoryItemView> onClick = null;

        public MemoryPieceAsset Asset => boundAsset;
        public float Multiplier => boundMultiplier;
        public bool HasAsset => boundAsset;

        private void Awake()
        {
            if (selectButton)
            {
                selectButton.onClick.AddListener(HandleClick);
            }
            UpdateVisuals(0);
            SetSelected(false);
        }

        private void OnDestroy()
        {
            if (selectButton)
            {
                selectButton.onClick.RemoveListener(HandleClick);
            }
        }

        public void Initialize(Action<MemoryPieceInventoryItemView> clickHandler)
        {
            onClick = clickHandler;
        }

        public void Bind(MemoryPieceAsset asset, float multiplier, int count)
        {
            boundAsset = asset;
            boundMultiplier = multiplier;
            UpdateVisuals(count);
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight)
            {
                selectionHighlight.SetActive(selected);
            }
        }

        private void UpdateVisuals(int count)
        {
            if (iconImage)
            {
                if (boundAsset && boundAsset.Icon)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = boundAsset.Icon;
                }
                else
                {
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                }
            }

            if (nameLabel)
            {
                if (boundAsset)
                {
                    string multiplierText = Mathf.Approximately(boundMultiplier, 1f)
                        ? string.Empty
                        : $" ×{boundMultiplier:0.##}";
                    nameLabel.text = $"{boundAsset.DisplayName}{multiplierText}";
                }
                else
                {
                    nameLabel.text = string.Empty;
                }
            }

            if (countLabel)
            {
                if (boundAsset && count > 1)
                {
                    countLabel.gameObject.SetActive(true);
                    countLabel.text = count.ToString();
                }
                else
                {
                    countLabel.text = string.Empty;
                    countLabel.gameObject.SetActive(false);
                }
            }
        }

        private void HandleClick()
        {
            onClick?.Invoke(this);
        }
    }
}
