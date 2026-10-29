using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using Frontend;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Equipment
{
    /// <summary>Six independently owned trigger/effect circuits, backed by existing combat content.</summary>
    public sealed class EquipmentLoadout : MonoBehaviour, IBuff
    {
        public const int SlotCount = 6;
        public readonly EquipmentSlot[] Slots = new EquipmentSlot[SlotCount];
        public readonly List<EquipmentItem> Items = new();
        public EquipmentCatalog Catalog { get; private set; }
        public Player Owner { get; private set; }
        public EquipmentTerminalView View { get; private set; }
        public event Action Changed;
        public event Action<int, float> Activated;
        public bool isStable => false;
        private int nextId = 1;
        private Transform runtimeRoot, staging;
        private readonly Circuit[] circuits = new Circuit[SlotCount];
        private bool initialized;
        private sealed class Circuit
        {
            public Board Trigger;
            public Polyomino Effect;
            public readonly HashSet<IEntityEventListener> Subscriptions = new();
        }

        public void Initialize(Player owner, EquipmentCatalog catalog)
        {
            if (initialized) return;
            initialized = true; Owner = owner; Catalog = catalog;
            for (int i = 0; i < SlotCount; i++) Slots[i] = new EquipmentSlot();
            runtimeRoot = new GameObject("Equipped circuits").transform;
            runtimeRoot.SetParent(transform, false);
            staging = new GameObject("Equipment staging").transform;
            staging.SetParent(transform, false); staging.gameObject.SetActive(false);
            if (owner.inventory)
            {
                foreach (var board in owner.inventory.getBoards) if (board) board.removeSelf();
                owner.inventory.show = false;
                owner.inventory.gameObject.SetActive(false);
            }
            Owner.stat.registerBuff(this);
            if (catalog && catalog.grantStartingKit)
                foreach (var definition in catalog.definitions) Acquire(definition);
            View = new GameObject("Equipment terminal", typeof(RectTransform)).AddComponent<EquipmentTerminalView>();
            View.Bind(this, catalog ? catalog.font : null);
        }

        private void Update()
        {
            if (View && Input.GetKeyDown(KeyCode.Escape)) View.Toggle();
        }

        public EquipmentItem Acquire(EquipmentDefinition definition)
        {
            if (definition == null || !definition.prefab) return null;
            var bonuses = new List<TriggerBonus>();
            if (definition.kind == EquipmentKind.Trigger)
            {
                var first = (TriggerBonusKind)UnityEngine.Random.Range(0, 9);
                bonuses.Add(Roll(first));
                if (UnityEngine.Random.value < .35f)
                {
                    var second = (TriggerBonusKind)(((int)first + UnityEngine.Random.Range(1, 9)) % 9);
                    bonuses.Add(Roll(second));
                }
            }
            var item = new EquipmentItem(nextId++, definition, bonuses.ToArray());
            Items.Add(item); Changed?.Invoke(); return item;
        }

        private static TriggerBonus Roll(TriggerBonusKind kind)
        {
            float amount = kind switch
            {
                TriggerBonusKind.Attack => UnityEngine.Random.Range(5, 16),
                TriggerBonusKind.Health => UnityEngine.Random.Range(1, 4) * 50,
                TriggerBonusKind.Defense => UnityEngine.Random.Range(5, 16),
                TriggerBonusKind.ProjectileCount => 1,
                TriggerBonusKind.Duration => UnityEngine.Random.Range(1, 3) * .5f,
                _ => UnityEngine.Random.Range(10, 26) / 100f
            };
            return new TriggerBonus { kind = kind, amount = amount };
        }

        public EquipmentItem GrantRandom(EquipmentKind kind)
        {
            if (!Catalog) return null;
            var candidates = Array.FindAll(Catalog.definitions, d => d.kind == kind && d.prefab);
            return candidates.Length == 0 ? null : Acquire(candidates[UnityEngine.Random.Range(0, candidates.Length)]);
        }

        public bool GrantLegacyEffect(Polyomino prefab)
        {
            if (!Catalog || !prefab) return false;
            foreach (var d in Catalog.definitions)
                if (d.kind == EquipmentKind.Effect && d.prefab == prefab.gameObject) { Acquire(d); return true; }
            // Chest content outside the initial catalog still has a real inventory entry.
            Acquire(new EquipmentDefinition { id = prefab.GetType().Name, title = prefab.Name,
                description = prefab.Description, kind = EquipmentKind.Effect, prefab = prefab.gameObject });
            return true;
        }

        public int EquippedAt(EquipmentItem item)
        {
            for (int i=0;i<SlotCount;i++) if (Slots[i].Trigger==item || Slots[i].Effect==item) return i;
            return -1;
        }

        public bool TryEquip(int index, EquipmentItem item, out string reason)
        {
            reason = "";
            if (index < 0 || index >= SlotCount || item == null || !Items.Contains(item)) { reason="장비를 선택하세요."; return false; }
            var slot = Slots[index];
            if (item.Definition.kind == EquipmentKind.Effect && slot.Trigger == null) { reason="트리거를 먼저 설치하세요."; return false; }
            int from = EquippedAt(item);
            if (from == index) return true;
            StopCircuit(index); if (from >= 0) StopCircuit(from);
            if (item.Definition.kind == EquipmentKind.Trigger)
            {
                var displaced = slot.Trigger; slot.Trigger = item;
                if (from >= 0)
                {
                    Slots[from].Trigger = displaced;
                    if (displaced == null) Slots[from].Effect = null;
                }
            }
            else
            {
                var displaced = slot.Effect; slot.Effect = item;
                if (from >= 0) Slots[from].Effect = displaced;
            }
            StartCircuit(index); if (from >= 0) StartCircuit(from);
            Notify(); return true;
        }

        public void Unequip(int index, EquipmentKind kind)
        {
            if (index < 0 || index >= SlotCount) return;
            StopCircuit(index);
            if (kind == EquipmentKind.Trigger) { Slots[index].Trigger=null; Slots[index].Effect=null; }
            else Slots[index].Effect=null;
            Notify();
        }

        private void StartCircuit(int index)
        {
            var slot = Slots[index]; if (!slot.IsComplete || !Owner) return;
            var circuit = new Circuit(); circuits[index] = circuit;
            var triggerGO = Instantiate(slot.Trigger.Definition.prefab, staging);
            circuit.Trigger = triggerGO.GetComponent<Board>();
            var effectGO = Instantiate(slot.Effect.Definition.prefab, staging);
            circuit.Effect = effectGO.GetComponent<Polyomino>();
            circuit.Effect.UseEquipmentMode();
            effectGO.transform.SetParent(runtimeRoot, false); effectGO.SetActive(true);
            circuit.Trigger.RuntimeActivation = (_, power) =>
            {
                if (circuits[index] != circuit || !Owner || Owner.stat.nowHp <= 0 || (View && View.IsOpen)) return;
                circuit.Subscriptions.RemoveWhere(l => !Owner.Listeners.Contains(l));
                var before = new HashSet<IEntityEventListener>(Owner.Listeners);
                try { circuit.Effect.trigger(Owner, power); }
                finally
                {
                    foreach (var listener in Owner.Listeners) if (!before.Contains(listener)) circuit.Subscriptions.Add(listener);
                }
                slot.ActivationCount++;slot.LastPower=power;
                Activated?.Invoke(index, power);
            };
            circuit.Trigger.registerTarget(Owner);
        }

        private void StopCircuit(int index)
        {
            if (Slots[index] != null) { Slots[index].ActivationCount=0; Slots[index].LastPower=0; }
            var c = circuits[index]; if (c == null) return;
            circuits[index] = null;
            if (c.Trigger) { c.Trigger.RuntimeActivation=null; c.Trigger.removeSelf(); Destroy(c.Trigger.gameObject); }
            foreach (var listener in c.Subscriptions)
            {
                if (!Owner) break;
                if (Owner.Listeners.Contains(listener)) listener.removeSelf();
                Owner.removeListener(listener);
                if (listener is IBuff buff) Owner.stat.removeBuff(buff);
            }
            if (c.Effect) { c.Effect.gameObject.SetActive(false); Destroy(c.Effect.gameObject); }
        }

        private void Notify()
        {
            if (Owner) Owner.RefreshEquipmentStats();
            Changed?.Invoke();
        }

        public void applyBuff(IStat stat)
        {
            if (stat is not EntityStat s) return;
            foreach (var slot in Slots)
            {
                if (slot == null || !slot.IsComplete) continue;
                foreach (var bonus in slot.Trigger.Bonuses)
                    switch (bonus.kind)
                    {
                        case TriggerBonusKind.Attack: s.increaseAtk += bonus.amount; break;
                        case TriggerBonusKind.AttackSpeed: s.fireSpeed += bonus.amount; break;
                        case TriggerBonusKind.ProjectileSpeed: s.bulletSpeed += bonus.amount; break;
                        case TriggerBonusKind.Cooldown: s.skillCooldownDecrease += bonus.amount; break;
                        case TriggerBonusKind.Health: s.maxHp += Mathf.RoundToInt(bonus.amount); break;
                        case TriggerBonusKind.Defense: s.baseDef += Mathf.RoundToInt(bonus.amount); break;
                        case TriggerBonusKind.ProjectileCount: s.bulletRate += bonus.amount; break;
                        case TriggerBonusKind.Duration: s.additionalDuration += bonus.amount; break;
                        case TriggerBonusKind.Range: s.skillRange += bonus.amount; break;
                    }
            }
        }

        public void removeSelf()
        {
            for(int i=0;i<SlotCount;i++) StopCircuit(i);
            if (Owner && Owner.stat != null) Owner.stat.removeBuff(this);
        }
        private void OnDisable()
        {
            if (!initialized) return;
            if (View) { View.Close(); View.gameObject.SetActive(false); }
            removeSelf();
        }
        private void OnEnable()
        {
            if (!initialized || !Owner || Owner.stat == null) return;
            Owner.stat.registerBuff(this);
            for (int i=0;i<SlotCount;i++) StartCircuit(i);
            if (View) View.gameObject.SetActive(true);
            Notify();
        }
        private void OnDestroy() { removeSelf(); if (View) Destroy(View.gameObject); }
    }
}
