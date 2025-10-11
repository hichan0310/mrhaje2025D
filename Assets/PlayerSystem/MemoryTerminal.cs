using UnityEngine;
using UI;

namespace PlayerSystem
{
    public class MemoryTerminal : MonoBehaviour, IInteractable
    {
        [System.Serializable]
        private struct MemoryGrant
        {
            public MemoryPieceAsset piece;
            public Vector2Int position;
            [Range(0.1f, 5f)] public float powerMultiplier;
        }

        [SerializeField] private MemoryGrant[] grants = new MemoryGrant[0];
        [SerializeField] private bool oneTimePickup = true;
        [SerializeField] private bool removeIfExists = false;
        [SerializeField] private bool openOverlayOnInteract = true;
        [SerializeField] private MemoryBoardOverlay overlayReference = null;

        private bool granted;

        public Vector3 WorldPosition => transform.position;

        public void Interact(Player player)
        {
            if (!player)
            {
                return;
            }

            var binder = player.GetComponent<PlayerMemoryBinder>();
            if (!binder)
            {
                return;
            }

            bool canGrant = !(oneTimePickup && granted);
            bool anyChange = false;
            if (canGrant)
            {
                foreach (var grant in grants)
                {
                    if (!grant.piece)
                    {
                        continue;
                    }

                    if (removeIfExists && binder.Board.Contains(grant.piece))
                    {
                        if (binder.RemovePiece(grant.piece))
                        {
                            anyChange = true;
                        }
                        continue;
                    }

                    float multiplier = grant.powerMultiplier <= 0f ? 1f : grant.powerMultiplier;
                    if (binder.TryAddPieceToInventory(grant.piece, multiplier))
                    {
                        anyChange = true;
                    }
                }

                if (anyChange)
                {
                    granted = true;
                }
            }

            if (openOverlayOnInteract)
            {
                var overlay = overlayReference ? overlayReference : FindObjectOfType<MemoryBoardOverlay>(true);
                overlay?.Open(binder);
            }
        }
    }
}
