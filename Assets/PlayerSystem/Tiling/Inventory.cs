using System;
using PlayerSystem;
using UnityEngine;
using UI;

namespace PlayerSystem.Tiling
{
    /// <summary>
    /// Lightweight bridge between the legacy inventory hotkey handling and the new
    /// <see cref="MemoryBoardOverlay"/> UI that renders memory pieces.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        private static readonly Cell[] EmptyCells = Array.Empty<Cell>();

        [Header("References")]
        [SerializeField] private PlayerMemoryBinder binder = null;
        [SerializeField] private MemoryBoardOverlay overlay = null;
        [Tooltip("Inventory가 활성화될 때 자동으로 오버레이를 연다.")]
        [SerializeField] private bool openOnEnable = false;

        private bool isOpen = false;

        /// <summary>
        /// 기존 코드와의 호환을 위해 남겨둔 프로퍼티. true로 설정하면 오버레이를 열고,
        /// false로 설정하면 닫는다.
        /// </summary>
        public bool show
        {
            get => isOpen;
            set
            {
                if (value)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }

        private void Awake()
        {
            if (!binder)
            {
                binder = GetComponent<PlayerMemoryBinder>() ?? GetComponentInParent<PlayerMemoryBinder>();
            }

            if (!overlay)
            {
                overlay = FindObjectOfType<MemoryBoardOverlay>(true);
            }
        }

        private void OnEnable()
        {
            if (openOnEnable && !isOpen)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            Close();
        }

        /// <summary>
        /// Inventory 키 입력에 대응하여 오버레이를 토글한다.
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (isOpen)
            {
                return;
            }

            if (!binder)
            {
                Debug.LogWarning("PlayerMemoryBinder가 없어 인벤토리를 열 수 없습니다.", this);
                return;
            }

            if (!EnsureOverlay())
            {
                Debug.LogWarning("MemoryBoardOverlay를 찾을 수 없어 인벤토리를 열 수 없습니다.", this);
                return;
            }

            isOpen = true;
            if (!overlay.gameObject.activeSelf)
            {
                overlay.gameObject.SetActive(true);
            }

            overlay.Open(binder);
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            if (overlay)
            {
                overlay.Close();
            }
        }

        /// <summary>
        /// 현재 인벤토리에 존재하는 피스의 모양(셀 좌표)을 rotation 단계에 맞춰 복사한다.
        /// </summary>
        public bool TryGetInventoryPieceShape(PlayerMemoryBinder.MemoryPieceInventoryItem item, int rotationSteps,
            out Cell[] cells)
        {
            return TryGetPieceShape(item.Asset, rotationSteps, out cells);
        }

        /// <summary>
        /// 특정 메모리 피스 에셋의 셀 배열을 반환한다.
        /// </summary>
        public bool TryGetPieceShape(MemoryPieceAsset asset, int rotationSteps, out Cell[] cells)
        {
            if (!asset)
            {
                cells = EmptyCells;
                return false;
            }

            var source = asset.GetTilingCells(rotationSteps);
            cells = CopyCells(source);
            return cells.Length > 0;
        }

        private static Cell[] CopyCells(System.Collections.Generic.IReadOnlyList<Cell> source)
        {
            if (source == null || source.Count == 0)
            {
                return EmptyCells;
            }

            var result = new Cell[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                result[i] = source[i];
            }

            return result;
        }

        private bool EnsureOverlay()
        {
            if (overlay)
            {
                return true;
            }

            overlay = FindObjectOfType<MemoryBoardOverlay>(true);
            return overlay;
        }
    }
}
