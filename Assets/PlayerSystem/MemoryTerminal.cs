// Assets/PlayerSystem/MemoryTerminal.cs
using UnityEngine;

namespace PlayerSystem
{
    public class MemoryTerminal : MonoBehaviour, IInteractable
    {
        [System.Serializable]
        private struct MemoryGrant
        {
            public MemoryPieceAsset piece;
            public float powerMultiplier;
        }

        [SerializeField] private MemoryGrant[] grants = new MemoryGrant[0];
        [SerializeField] private bool oneTimePickup = true;
        [SerializeField] private bool removeIfExists = false;
        [Header("Overlay Opening")]
        [SerializeField] private bool openOverlayOnInteract = true;
        [SerializeField] private MemoryBoardOverlay overlayReference = null;

        private bool granted;

        public Vector3 WorldPosition => transform.position;

        public void Interact(Player player)
        {
            Debug.Log("[MemoryTerminal] Interact called");

            if (!player) return;
            var binder = player.GetComponent<PlayerMemoryBinder>();
            if (!binder) return;

            bool changed = false;
            if (!(oneTimePickup && granted))
            {
                foreach (var g in grants)
                {
                    if (!g.piece) continue;

                    if (removeIfExists)
                    {
                        if (binder.RemovePiece(g.piece)) { changed = true; continue; }
                    }

                    if (binder.TryAddPieceToInventory(g.piece, g.powerMultiplier <= 0 ? 1f : g.powerMultiplier))
                        changed = true;
                }

                if (changed) granted = true;
            }

            //  Lite 버전 오버레이 열기
            if (openOverlayOnInteract)
            {
                var overlay = overlayReference
                              ? overlayReference
                              : FindObjectOfType<MemoryBoardOverlay>(true);

                if (overlay)
                {
                    if (!overlay.gameObject.activeSelf)
                        overlay.gameObject.SetActive(true);
                    overlay.Open(binder);
                }
                else
                {
                    Debug.LogWarning("[MemoryTerminal] No MemoryBoardOverlayLite found in scene.");
                }
            }
        }
    }
}
