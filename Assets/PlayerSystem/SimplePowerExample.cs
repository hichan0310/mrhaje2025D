// Assets/PlayerSystem/Triggers/PowerPolicies.cs
using System;
using EntitySystem.Events;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem.Triggers
{
    /// <summary>
    /// 이벤트 -> power 변환 정책.
    /// 인터페이스 기반으로 안전하게 값을 읽어 밸런스를 중앙집중화.
    /// </summary>
    public static class PowerPolicies
    {
        public static float? One(EventArgs e) => 1f;

        public static float? Select(EventArgs e)
        {
            if (e == null) return null;

            if (e is IDamageInfo di) return Mathf.Max(1f, di.damage);
            if (e is IPercentInfo pi) return Mathf.Max(1f, pi.percent);
            if (e is ITimeInfo ti) return Mathf.Max(1f, ti.time);

            return null;
        }

        public static Func<EventArgs, float?> Const(float k) => _ => k < 0f ? 0f : k;
    }
}
