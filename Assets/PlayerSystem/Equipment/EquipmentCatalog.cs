using System;
using PlayerSystem.Tiling;
using TMPro;
using UnityEngine;

namespace PlayerSystem.Equipment
{
    public enum EquipmentKind { Trigger, Effect }
    public enum TriggerBonusKind { Attack, AttackSpeed, ProjectileSpeed, Cooldown, Health, Defense, ProjectileCount, Duration, Range }

    [Serializable]
    public sealed class EquipmentDefinition
    {
        public string id;
        public string title;
        [TextArea(2, 8)] public string description;
        public string powerSummary;
        public EquipmentKind kind;
        public GameObject prefab;
        public Sprite icon;
        public int glyph;
    }

    [CreateAssetMenu(menuName = "Player/Equipment/Catalog")]
    public sealed class EquipmentCatalog : ScriptableObject
    {
        public TMP_FontAsset font;
        public EquipmentDefinition[] definitions = Array.Empty<EquipmentDefinition>();
        [Tooltip("Initial review kit. Disable for reward-driven progression.")]
        public bool grantStartingKit = true;
    }

    [Serializable]
    public sealed class TriggerBonus
    {
        public TriggerBonusKind kind;
        public float amount;
        public string Description => kind switch
        {
            TriggerBonusKind.Attack => $"공격력 +{amount:0}%",
            TriggerBonusKind.AttackSpeed => $"공격 속도 +{amount * 100:0}%",
            TriggerBonusKind.ProjectileSpeed => $"탄환 속도 +{amount * 100:0}%",
            TriggerBonusKind.Cooldown => $"쿨다운 감소 +{amount:0.00}",
            TriggerBonusKind.Health => $"최대 체력 +{amount:0}",
            TriggerBonusKind.ProjectileCount => $"탄환 수 +{amount:0}",
            TriggerBonusKind.Duration => $"추가 지속시간 +{amount:0.0}",
            TriggerBonusKind.Range => $"스킬 범위 +{amount * 100:0}%",
            _ => $"방어력 +{amount:0}"
        };
    }

    public sealed class EquipmentItem
    {
        public int Id { get; }
        public EquipmentDefinition Definition { get; }
        public TriggerBonus[] Bonuses { get; }
        public string BonusDescription => string.Join("  /  ", Array.ConvertAll(Bonuses, b => b.Description));
        public EquipmentItem(int id, EquipmentDefinition definition, TriggerBonus[] bonuses)
        {
            Id = id; Definition = definition;
            Bonuses = definition.kind == EquipmentKind.Trigger ? bonuses ?? Array.Empty<TriggerBonus>() : Array.Empty<TriggerBonus>();
        }
    }

    public sealed class EquipmentSlot
    {
        public EquipmentItem Trigger { get; internal set; }
        public EquipmentItem Effect { get; internal set; }
        public bool IsComplete => Trigger != null && Effect != null;
        public int ActivationCount { get; internal set; }
        public float LastPower { get; internal set; }
    }
}
