using EntitySystem;
using GameBackend;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Component that wires the serialized memory board to the runtime player instance.
    /// </summary>
    public class PlayerMemoryBinder : MonoBehaviour
    {
        [SerializeField] private Player player = null;
        [SerializeField] private MemoryBoard board = new();
        [SerializeField] private float globalPowerScale = 1f;

        public MemoryBoard Board => board;

        private void Awake()
        {
            if (!player)
            {
                player = GetComponent<Player>();
            }

            board.Initialize(this);
        }

        private void Update()
        {
            board.Tick(TimeManager.deltaTime);
        }

        public void Trigger(ActionTriggerType triggerType, float basePower = 1f)
        {
            if (!player)
            {
                return;
            }

            float power = Mathf.Max(0f, basePower * globalPowerScale);
            board.Trigger(triggerType, player, power);
        }

        public bool TryAddPiece(MemoryPieceAsset asset, Vector2Int origin, float multiplier = 1f, bool locked = false)
        {
            return board.TryAddPiece(asset, origin, multiplier, locked);
        }

        public bool RemovePiece(MemoryPieceAsset asset)
        {
            return board.RemovePiece(asset);
        }
    }
}
