# Project Structure & Code Review

## High-level Architecture
- **EntitySystem** contains the runtime model for combatants. `Entity` is a `MonoBehaviour` that
  maintains an `EntityStat` instance, keeps a list of `IEntityEventListener`s, and forwards Unity
  `Update` ticks to each listener so they can react to time and events.【F:Assets/EntitySystem/Entity.cs†L10-L33】
- **GameBackend** currently only exposes `TimeManager`, a static helper that scales global time and
  keeps references to every registered `Entity` so their animator speed can be adjusted as the time
  scale changes.【F:Assets/EntitySystem/TimeManager.cs†L7-L38】
- **EntitySystem.Events** defines the event bus that glues combat together. `DamageGiveEvent`
  notifies both the attacker and the target, the target applies mitigation in `Entity.takeDamage`,
  and then raises a `DamageTakeEvent` that can produce UI feedback via `DamageEventManager` and
  eventually an `EntityDieEvent` when HP drops to zero.【F:Assets/EntitySystem/Events/DamageGiveEvent.cs†L6-L31】【F:Assets/EntitySystem/Entity.cs†L55-L60】【F:Assets/EntitySystem/Events/DamageTakeEvent.cs†L5-L30】【F:Assets/EntitySystem/DamageEventManager.cs†L6-L29】
- **EntitySystem.StatSystem** models derived combat stats and modifiers. `EntityStat.calculate()`
  clones the current stat, lets each non-stable buff mutate it, and returns the modified snapshot.
  Both damage calculation helpers (`calculateTrueDamage`/`calculateTakenDamage`) short-circuit to a
  recalculated snapshot when mutable buffs are present so that stacking effects are applied in order.【F:Assets/EntitySystem/StatSystem/EntityStat.cs†L45-L165】
- **EntitySystem.BuffTypes** provides base classes for different buff life-cycle patterns
  (`Buff`, `BuffOnce`, `BuffStackIndependent`, `BuffStackLimited`). They encapsulate the
  bookkeeping for attaching an `IBuff` to an `Entity` and expiring the effect over time.【F:Assets/EntitySystem/BuffTypes/Buff.cs†L7-L40】【F:Assets/EntitySystem/BuffTypes/BuffOnce.cs†L8-L52】【F:Assets/EntitySystem/BuffTypes/BuffStackIndependent.cs†L8-L129】【F:Assets/EntitySystem/BuffTypes/BuffStackLimited.cs†L8-L78】
- **PlayerSystem** demonstrates how triggers and effects compose on top of the entity layer. A
  `Trigger` listens for entity events and, when its internal logic allows, broadcasts power values
  to one or more `ITriggerEffect` implementations such as the sample buff and projectile effects.【F:Assets/PlayerSystem/Trigger.cs†L6-L14】【F:Assets/PlayerSystem/Triggers/SimpleTriggerExample.cs†L6-L39】【F:Assets/PlayerSystem/Effects/SimpleEffectExample1.cs†L7-L49】【F:Assets/PlayerSystem/Effects/SimpleEffectExample2.cs†L7-L23】

## Event & Buff Flow
1. `DamageGiveEvent.trigger()` notifies the attacker and calls `Entity.takeDamage()` on the target.【F:Assets/EntitySystem/Events/DamageGiveEvent.cs†L25-L30】
2. The target clones its stats, computes mitigated damage, applies it, and emits a
   `DamageTakeEvent`. UI listeners (e.g. `DamageEventManager`) spawn feedback and the entity forwards
   the event to any registered listeners. If HP reaches zero, `EntityDieEvent` is raised so other
   systems can respond.【F:Assets/EntitySystem/Entity.cs†L55-L61】【F:Assets/EntitySystem/Events/DamageTakeEvent.cs†L22-L30】
3. Buff base classes attach themselves as listeners and stat modifiers. When `Entity.update()` runs,
   each buff's `update` method receives the delta time so duration-based effects can expire and
   unregister cleanly.【F:Assets/EntitySystem/Entity.cs†L28-L33】【F:Assets/EntitySystem/BuffTypes/BuffOnce.cs†L36-L48】【F:Assets/EntitySystem/BuffTypes/BuffStackIndependent.cs†L117-L125】

## Code Review Findings & Suggestions
- **HP never decreases on damage** – `Entity.takeDamage` mutates the cloned stat returned by
  `calculate()`, so the entity's real `stat.nowHp` stays unchanged. Call `this.stat.takeDamage(dmg)`
  (or update the original instance) to persist HP loss before raising the take event.【F:Assets/EntitySystem/Entity.cs†L55-L60】【F:Assets/EntitySystem/StatSystem/EntityStat.cs†L45-L165】
- **First-time stack buff registration skips listeners** – `BuffStackIndependent.registerTarget`
  only registers the buff with the entity when the target already exists in `targets`. New targets
  never add the listener or modifier, so the buff never activates. Ensure the listener/stat
  registration also runs in the first-time branch.【F:Assets/EntitySystem/BuffTypes/BuffStackIndependent.cs†L85-L98】
- **Trigger cooldown never recovers** – `SimpleTriggerExample.update` decreases the timer only when
  the entity passed into `update` is *not* the registered target, which never happens. Flip the
  condition (or remove it) so the cooldown counts down while attached to its owner.【F:Assets/PlayerSystem/Triggers/SimpleTriggerExample.cs†L32-L38】
- **TimeManager retains destroyed entities** – Entities register in `Start` but nothing calls
  `TimeManager.removerEntity` during `OnDestroy`, so destroyed objects leave stale references and the
  animator speed sync keeps running against null entries. Hook into `OnDestroy` (or use `OnDisable`)
  to unregister and avoid leaks.【F:Assets/EntitySystem/Entity.cs†L17-L44】【F:Assets/EntitySystem/TimeManager.cs†L25-L38】
- **Stat arrays rely on manual inspector setup** – `EntityStat` expects `dmgUp`, `dmgDrain`,
  `dmgTakeUp`, and `dmgAdd` to be non-null, but there is no constructor ensuring that when an
  instance is created from scratch. Accessing `dmgUp[(int)AtkTags.all]` will throw unless every array
  is populated ahead of time. Initialize them (e.g., in a default constructor or `Awake`) or guard
  with null checks.【F:Assets/EntitySystem/StatSystem/EntityStat.cs†L36-L133】
- **Projectile effect lacks target assignment** – `SimpleEffectExample2` instantiates a fireball but
  never sets a destination. Additionally, `Example2FireBall` exposes `giveDamage` as a private method
  that never runs, so the projectile cannot deliver damage yet. Flesh out the movement and collision
  logic, and expose a way to trigger `giveDamage` when it hits something.【F:Assets/PlayerSystem/Effects/SimpleEffectExample2.cs†L12-L23】【F:Assets/PlayerSystem/Effects/Example2FireBall.cs†L8-L16】

## Suggested Next Steps
- Add lifecycle hooks on `Entity` to unregister from `TimeManager` and listeners when disabled or
  destroyed.
- Provide unit tests (or editor tests) around the damage pipeline to catch regressions such as the
  HP mutation bug.
- Expand the buff base classes with clear contracts (e.g., document expected args, stack limits) and
  consider extracting common timer logic to avoid duplication between `BuffOnce` and
  `BuffStackIndependent`.
- Convert magic numbers (damage coefficients, cooldown seconds) to serialized fields so designers can
  tweak them without recompiling.
