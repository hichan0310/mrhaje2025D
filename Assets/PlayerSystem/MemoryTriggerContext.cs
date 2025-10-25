using System;
using System.Collections.Generic;
using EntitySystem;
using PlayerSystem.Weapons;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// Runtime context that aggregates enhancements for a single trigger activation.
    /// </summary>
    public sealed class MemoryTriggerContext
    {
        private readonly List<Action<Projectile, float>> projectileCallbacks = new();

        internal MemoryTriggerContext(PlayerMemoryBinder binder, ActionTriggerType trigger, MemoryBoard board, float basePower)
        {
            Binder = binder;
            Trigger = trigger;
            Board = board;
            BasePower = basePower;
        }

        public PlayerMemoryBinder Binder { get; }
        public ActionTriggerType Trigger { get; }
        public MemoryBoard Board { get; }
        public float BasePower { get; }
        public MemoryPieceAsset? CurrentPiece { get; private set; }
        public float CurrentPiecePower { get; private set; }
        public float DamageBonusPercent { get; private set; }
        public float KnockbackForce { get; private set; }
        public float RecoilForce { get; private set; }

        public void AddDamageBonusPercent(float percent)
        {
            DamageBonusPercent += percent;
        }

        public void AddKnockbackForce(float force)
        {
            KnockbackForce += force;
        }

        public void AddRecoilForce(float force)
        {
            RecoilForce += force;
        }

        public void RegisterProjectileCallback(Action<Projectile, float> callback)
        {
            if (callback != null)
            {
                projectileCallbacks.Add(callback);
            }
        }

        internal void SetCurrentPiece(MemoryPieceAsset asset, float power)
        {
            CurrentPiece = asset;
            CurrentPiecePower = power;
        }

        internal void ApplyToProjectile(Projectile projectile)
        {
            if (!projectile)
            {
                return;
            }

            projectile.ApplyTriggerEnhancements(DamageBonusPercent, KnockbackForce, RecoilForce);

            foreach (var callback in projectileCallbacks)
            {
                try
                {
                    callback?.Invoke(projectile, CurrentPiecePower);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        internal void Complete()
        {
            projectileCallbacks.Clear();
            CurrentPiece = null;
            CurrentPiecePower = 0f;
        }

        public static bool TryGetActive(Entity entity, out MemoryTriggerContext context)
        {
            context = null;
            if (!entity)
            {
                return false;
            }

            if (!entity.TryGetComponent(out PlayerMemoryBinder binder))
            {
                return false;
            }

            return binder.TryGetContext(out context);
        }
    }
}
