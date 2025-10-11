using UnityEngine;

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

            if (oneTimePickup && granted)
            {
                return;
            }

            foreach (var grant in grants)
            {
                if (!grant.piece)
                {
                    continue;
                }

                if (removeIfExists && binder.Board.Contains(grant.piece))
                {
                    binder.RemovePiece(grant.piece);
                    continue;
                }

                float multiplier = grant.powerMultiplier <= 0f ? 1f : grant.powerMultiplier;
                Debug.Log("interact");
                binder.TryAddPiece(grant.piece, grant.position, multiplier);
            }

            granted = true;
        }
    }
}
