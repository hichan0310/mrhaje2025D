using EntitySystem;
using UnityEngine;

namespace PlayerSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerActionController))]
    public class Player : Entity
    {
        [SerializeField]
        [Tooltip("Optional trigger that is automatically registered to this player on start.")]
        [SerializeReference]
        private Trigger defaultTrigger;

        public PlayerActionController Actions { get; private set; }

        protected override void Start()
        {
            base.Start();
            Actions = GetComponent<PlayerActionController>();
            RegisterDefaultTrigger();
        }

        private void OnDestroy()
        {
            defaultTrigger?.removeSelf();
        }

        private void RegisterDefaultTrigger()
        {
            if (defaultTrigger == null)
            {
                return;
            }

            defaultTrigger.registerTarget(this);
        }
    }
}
