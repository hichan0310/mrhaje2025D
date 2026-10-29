using Frontend;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public abstract class Weapon:MonoBehaviour, IEntityEventListener
    {
        protected Player player;
        [Header("Weapon HUD")]
        [SerializeField] private WeaponHudView hudPrefab;
        [SerializeField] private Sprite hudIcon;
        private WeaponHudView hudInstance;

        public virtual void WriteHud(WeaponHudState state)
        {
            state.Title = name;
            state.Icon = hudIcon;
            state.Enabled = isActiveAndEnabled && player && player.stat != null && player.stat.nowHp > 0;
        }

        private void ShowHud()
        {
            if (!Application.isPlaying || !player || !isActiveAndEnabled || !hudPrefab) return;
            if (!hudInstance) hudInstance = Instantiate(hudPrefab);
            hudInstance.gameObject.SetActive(true);
            hudInstance.Bind(this);
        }

        protected virtual void OnDestroy()
        {
            if (hudInstance) Destroy(hudInstance.gameObject);
        }
        public AimSupport aimSupport;
        
        public abstract void fire();
        public abstract void skill();
        public abstract void ultimate();
        public abstract void eventActive(EventArgs eventArgs);

        public virtual void registerTarget(Entity target, object args = null)
        {
            removeSelf();
            player = null;
            if (target is Player p)
            {
                player = p;
                player.registerListener(this);
            }
            else Debug.LogWarning(target + " is not a player");
            if (aimSupport) aimSupport.player = player;
            ShowHud();
        }

        public void removeSelf()
        {
            if (player) player.removeListener(this);
            if (hudInstance) hudInstance.gameObject.SetActive(false);
        }
        protected virtual void OnEnable()
        {
            if (player) player.registerListener(this);
            ShowHud();
        }
        protected virtual void OnDisable() => removeSelf();
        public abstract void update(float deltaTime, Entity target);
    }
}
