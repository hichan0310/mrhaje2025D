using System;
using System.Collections.Generic;
using EntitySystem;
using PlayerSystem.Weapons;
using UnityEngine;

namespace PlayerSystem
{
    /// <summary>
    /// (구) 트리거 1회 발동 동안 누적되는 보정값 컨텍스트.
    /// 현재 구조에서는 바인더가 컨텍스트를 생성/완주하지 않으므로,
    /// TryGetActive는 항상 false를 반환하게 됩니다(호환 션트).
    /// 투사체 보정/콜백 등 기존 인터페이스는 유지합니다.
    /// </summary>
    public sealed class MemoryTriggerContext
    {
        private readonly List<Action<Projectile, float>> projectileCallbacks = new();

        // (구) ActionTriggerType 제거: 생성자에서 더 이상 트리거 타입을 받지 않습니다.
        // 현재 구조에서는 외부에서 생성하지 않도록 internal 유지/미사용.
        internal MemoryTriggerContext(PlayerMemoryBinder binder, MemoryBoard board, float basePower)
        {
            Binder = binder;
            Board = board;   // null 허용
            BasePower = basePower;
        }

        public PlayerMemoryBinder Binder { get; }
        public MemoryBoard Board { get; }
        public float BasePower { get; }

        public MemoryPieceAsset? CurrentPiece { get; private set; }
        public float CurrentPiecePower { get; private set; }
        public float DamageBonusPercent { get; private set; }
        public float KnockbackForce { get; private set; }
        public float RecoilForce { get; private set; }

        public void AddDamageBonusPercent(float percent) => DamageBonusPercent += percent;
        public void AddKnockbackForce(float force) => KnockbackForce += force;
        public void AddRecoilForce(float force) => RecoilForce += force;

        public void RegisterProjectileCallback(Action<Projectile, float> callback)
        {
            if (callback != null) projectileCallbacks.Add(callback);
        }

        internal void SetCurrentPiece(MemoryPieceAsset asset, float power)
        {
            CurrentPiece = asset;
            CurrentPiecePower = power;
        }

        internal void ApplyToProjectile(Projectile projectile)
        {
            if (!projectile) return;

            projectile.ApplyTriggerEnhancements(DamageBonusPercent, KnockbackForce, RecoilForce);

            foreach (var callback in projectileCallbacks)
            {
                try { callback?.Invoke(projectile, CurrentPiecePower); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        internal void Complete()
        {
            projectileCallbacks.Clear();
            CurrentPiece = null;
            CurrentPiecePower = 0f;
        }

        /// <summary>
        /// (구) 호환 API: 현재는 항상 false를 반환합니다.
        /// </summary>
        public static bool TryGetActive(Entity entity, out MemoryTriggerContext context)
        {
            context = null;
            if (!entity) return false;
            if (!entity.TryGetComponent(out PlayerMemoryBinder binder)) return false;
            // 바인더의 션트가 항상 false 반환
            return binder.TryGetContext(out context);
        }
    }
}
