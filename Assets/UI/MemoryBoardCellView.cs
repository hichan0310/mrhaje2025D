using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayerSystem;

namespace UI
{
    public class MemoryBoardCellView : MonoBehaviour
    {
        [SerializeField] private Button button = null;
        [SerializeField] private Image backgroundImage = null;
        [SerializeField] private Image reinforcementHighlight = null;
        [SerializeField] private Image pieceIcon = null;
        [SerializeField] private TMP_Text pieceLabel = null;
        [SerializeField] private GameObject lockIndicator = null;

        private Color emptyColor = Color.gray;
        private Color occupiedColor = Color.white;
        private Color originColor = Color.yellow;
        private Color reinforcementColor = Color.green;

        public event Action<MemoryBoardCellView> Clicked;

        public Vector2Int Coordinates { get; private set; }

        public void Initialize(Vector2Int coordinates)
        {
            Coordinates = coordinates;
            if (button)
            {
                button.onClick.RemoveListener(OnButtonClicked);
                button.onClick.AddListener(OnButtonClicked);
            }
            Clear();
        }

        public void SetColors(Color empty, Color occupied, Color origin, Color reinforcement)
        {
            emptyColor = empty;
            occupiedColor = occupied;
            originColor = origin;
            reinforcementColor = reinforcement;
            UpdateBackground(emptyColor);
        }

        public void Clear()
        {
            UpdateBackground(emptyColor);
            if (pieceIcon)
            {
                pieceIcon.enabled = false;
                pieceIcon.sprite = null;
            }
            if (pieceLabel)
            {
                pieceLabel.text = string.Empty;
                pieceLabel.gameObject.SetActive(false);
            }
            if (reinforcementHighlight)
            {
                reinforcementHighlight.enabled = false;
            }
            if (lockIndicator)
            {
                lockIndicator.SetActive(false);
            }
        }

        public void SetReinforced(bool reinforced, float _)
        {
            if (!reinforcementHighlight)
            {
                return;
            }

            reinforcementHighlight.enabled = reinforced;
            if (reinforcementHighlight.enabled)
            {
                reinforcementHighlight.color = reinforcementColor;
            }
        }

        public void SetPiece(MemoryPieceAsset asset, float multiplier, bool locked, bool isOrigin)
        {
            UpdateBackground(isOrigin ? originColor : occupiedColor);

            if (pieceIcon)
            {
                if (asset && asset.Icon)
                {
                    pieceIcon.enabled = true;
                    pieceIcon.sprite = asset.Icon;
                }
                else
                {
                    pieceIcon.enabled = false;
                    pieceIcon.sprite = null;
                }
            }

            if (pieceLabel)
            {
                if (asset && isOrigin)
                {
                    pieceLabel.gameObject.SetActive(true);
                    pieceLabel.text = Mathf.Approximately(multiplier, 1f)
                        ? asset.DisplayName
                        : $"{asset.DisplayName}\n×{multiplier:0.##}";
                }
                else
                {
                    pieceLabel.text = string.Empty;
                    pieceLabel.gameObject.SetActive(false);
                }
            }

            if (lockIndicator)
            {
                lockIndicator.SetActive(locked);
            }
        }

        private void UpdateBackground(Color color)
        {
            if (backgroundImage)
            {
                backgroundImage.color = color;
            }
        }

        private void OnButtonClicked()
        {
            Clicked?.Invoke(this);
        }
    }
}
