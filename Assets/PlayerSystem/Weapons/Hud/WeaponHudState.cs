using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public enum WeaponHudAccent { Skill, Ultimate, Ammo, Effect }

    /// <summary>A weapon describes its own resources; the HUD does not inspect weapon subclasses.</summary>
    public readonly struct WeaponHudEntry
    {
        public readonly string Key, Label, Value, Status;
        public readonly float Fill, Threshold;
        public readonly int Charges, Capacity;
        public readonly bool Ready, Active, ShowGauge, Compact;
        public readonly WeaponHudAccent Accent;

        public WeaponHudEntry(string key, string label, string value, string status, float fill,
            WeaponHudAccent accent, bool ready = false, bool active = false, float threshold = 0,
            int charges = 0, int capacity = 0, bool showGauge = true, bool compact = false)
        {
            Key = key; Label = label; Value = value; Status = status;
            Fill = Mathf.Clamp01(fill); Threshold = Mathf.Clamp01(threshold);
            Charges = Mathf.Max(0, charges); Capacity = Mathf.Max(0, capacity);
            Ready = ready; Active = active; Accent = accent;
            ShowGauge = showGauge; Compact = compact;
        }

        public static WeaponHudEntry Skill(string label, int charges, int capacity, float remaining,
            float duration, string blocked = "", bool active = false)
        {
            bool full = charges >= capacity;
            string status = !string.IsNullOrEmpty(blocked) ? blocked : full ? "충전 완료" :
                charges > 0 ? $"사용 가능 · 다음 충전 {Mathf.Max(0, remaining):0.0}초" : $"충전 중 · {Mathf.Max(0, remaining):0.0}초";
            return new WeaponHudEntry("E", label, $"{charges} / {capacity}", status,
                full ? 1 : 1 - remaining / Mathf.Max(0.1f, duration), WeaponHudAccent.Skill,
                charges > 0 && string.IsNullOrEmpty(blocked), active, charges: charges, capacity: capacity);
        }

        public static WeaponHudEntry Ultimate(string label, float energy, float maximum, float cost,
            string blocked = "", bool active = false)
        {
            bool ready = energy >= cost && string.IsNullOrEmpty(blocked);
            string status = !string.IsNullOrEmpty(blocked) ? blocked : ready ? $"사용 가능 · {cost:0} 소모" :
                $"사용까지 {Mathf.CeilToInt(Mathf.Max(0, cost - energy))} 에너지";
            return new WeaponHudEntry("Q", label, $"{Mathf.FloorToInt(energy)} / {maximum:0}", status,
                energy / Mathf.Max(1, maximum), WeaponHudAccent.Ultimate, ready, active,
                cost / Mathf.Max(1, maximum));
        }
    }

    public sealed class WeaponHudState
    {
        public string Title, Subtitle;
        public Sprite Icon;
        public bool Enabled = true;
        public readonly List<WeaponHudEntry> Entries = new();

        public void Clear()
        {
            Title = Subtitle = string.Empty;
            Icon = null;
            Enabled = true;
            Entries.Clear();
        }
    }
}
