# Skill Tree System — How it works, extend it, and wire it up

## Overview

The skill tree is data‑driven and UI‑agnostic:

- Node data is authored as `ScriptableObject` assets (`SkillNodeDefinition`).
- Unlock state and spend logic live in a static controller (`SkillTreeState`).
- Effects are applied either via data‑driven stat modifiers (`SkillEffectsRunner` + `PlayerSkillHooks`) or legacy `OnApply` events for bespoke behaviors.
- UI binds nodes to toggles using `SkillNodeBinding` and shows available points via `SkillPointsLabel`.

Key scripts:

- `Assets/Scripts/UI/SkillTree/SkillNodeDefinition.cs`
- `Assets/Scripts/UI/SkillTree/SkillNodeDatabase.cs`
- `Assets/Scripts/UI/SkillTree/SkillTreeState.cs`
- `Assets/Scripts/UI/SkillTree/SkillNodeBinding.cs`
- `Assets/Scripts/UI/SkillTree/SkillPointsLabel.cs`
- `Assets/Scripts/UI/SkillTree/SkillEffectsRunner.cs`
- `Assets/Scripts/UI/SkillTree/PlayerSkillHooks.cs`
- `Assets/Scripts/UI/SkillTree/PlayerSkillNodeApplier.cs` (example of bespoke unlock)
- `Assets/Scripts/UI/SkillTree/VelocityScaleBySpeed.cs` (example effect toggled by a node)

Related systems:

- `Assets/Scripts/Player/PlayerProfile.cs` — persists `UnspentSkillPoints`.
- `Assets/Scripts/Player/PlayerXP.cs` — awards skill points on level ups.

## Data model (authoring nodes)

Create node assets under a Resources path so the database can discover them, e.g. `Assets/Resources/SkillNodes/`.

`SkillNodeDefinition` important fields:

- `id`: unique string key for persistence and references.
- `description`: tooltip text.
- `parents` / `parentIds`: prerequisites. Prefer `parentIds` for data import/export and stability.
- `nodeType`: Minor or Major.
- `statModifiers`: list of data‑driven effects with fields `(StatType, ModifierOp, value)`.
- `cost`: skill points required (default 1).
- `OnApply` (legacy): optional UnityEvent for bespoke or keystone behavior.

Supported data‑driven stats (extendable): `MaxHealth`, `ReflectFlat`, `ReflectPercent`, `RegenPerSecond`.

## Runtime: state, persistence, and spending

- `SkillTreeState` persists unlocks in `PlayerPrefs` under `skilltree.unlocked.ids` (pipe‑separated ids).
- Public API highlights:
  - `IsUnlocked(node)`, `ParentsSatisfied(node)`, `CanUnlock(node)`, `TryUnlock(node)`.
  - `RemainingPoints` reads from `PlayerProfile.UnspentSkillPoints`.
  - `SpentPoints` counts unlocked nodes (assumes `cost=1`).
  - `OnUnlocked` event fires when a node unlocks.
  - Helpers: `ExportCsv()`, `ImportCsv(csv)`, `ClearAllUnlocks()`.

Skill points flow:

- `PlayerXP` increments an internal counter and updates `PlayerProfile.UnspentSkillPoints` on level up.
- UI shows points via `SkillPointsLabel`.

## Applying effects

There are two complementary paths:

1) Data‑driven modifiers
   - `SkillEffectsRunner` listens for `SkillTreeState.OnUnlocked` and also applies all already‑unlocked nodes on `Start()`.
   - It forwards each `SkillNodeDefinition.StatModifier` to `PlayerSkillHooks.ApplyStatModifier` and calls `PlayerSkillHooks.Recompute()` afterward.
   - `PlayerSkillHooks` owns runtime totals and knows how to mutate the `Player` (health clamp, recompute stats, regen, reflect, etc.).

2) Bespoke/keystone events (legacy)
   - Wire per‑node `OnApply` UnityEvents or use `PlayerSkillNodeApplier` as an example to toggle special systems (e.g., enable `VelocityScaleBySpeed`, grant flat health/reflect/regen).

Recommendation: Prefer data‑driven `statModifiers` for numeric stat changes. Use bespoke hooks only for behaviors that cannot be expressed as a stat.

## UI wiring (radial tree or any layout)

Minimum setup (prefab example: `Assets/Input/Prefabs/UI/SkillTree/SkillTree Canvas.prefab`):

1) Node toggles
   - Add `SkillNodeBinding` to each UI node element (e.g., a `Toggle`).
   - Assign `data` to the `SkillNodeDefinition` asset and drag the `Toggle` reference.
   - `SkillNodeBinding` will:
     - Disable interaction until parents are unlocked and enough points remain.
     - On toggle ON, call `SkillTreeState.TryUnlock(data)`; if denied, it reverts the toggle.
     - Keep unlocked nodes ON and non‑refundable (unless you add refund support).
     - Show tooltips via `UITooltip` using `description` or `tooltipOverride`.

2) Points label
   - Add `SkillPointsLabel` to a `TMP_Text` in the tree header to display `{total}/{remaining}`.

3) Toggle the panel
   - Use your `UIToggleHotkey`/UI system to open/close the skill tree panel (e.g., bind an action like `UI/ToggleSkillTree`).

## How to add a new node

1) In Project window: Create → SkillTree → Skill Node.
2) Place it under `Assets/Resources/SkillNodes/` (or any `Resources` folder).
3) Fill fields:
   - `id` (unique), `description`, `nodeType`.
   - Set `parentIds` to prerequisite `id`s (or assign `parents` for editor‑only linking).
   - Add `statModifiers` for numeric effects (e.g., `MaxHealth Add 25`).
   - Optional: add `OnApply` listeners for bespoke effects.
4) In the skill tree UI prefab/scene, duplicate a node button and assign its `SkillNodeBinding.data` to this asset.

## How to wire the system in a scene

1) Ensure a `Player` exists and has `PlayerSkillHooks` (add if missing). Assign its `player` reference.
2) Add one `SkillEffectsRunner` to the scene (e.g., on the UI root or a game manager). It will discover unlocked nodes and apply effects on load.
3) Include your Skill Tree UI (e.g., drop in `SkillTree Canvas.prefab`) and wire `SkillNodeBinding` components to assets.
4) Make sure you are awarding skill points:
   - `PlayerXP` should call `PlayerProfile.UnspentSkillPoints++` on level up (already implemented).
5) Optional: Add a `PlayerSkillNodeApplier` if you have a special keystone node that needs bespoke behavior not covered by `statModifiers`.

## Extending the system

- Add new `StatType` entries to `SkillNodeDefinition` and handle them in `PlayerSkillHooks.ApplyStatModifier`.
- Add new visuals: create custom UI prefabs that still host a `Toggle` and `SkillNodeBinding`.
- Add refunds: extend `SkillTreeState` to support `allowRefund` and subtract points/state accordingly.
- Import/Export: use `SkillTreeState.ExportCsv()`/`ImportCsv()` for save systems.

## Troubleshooting

- Node won’t unlock: check `parentIds` correctness (typos), ensure points available, verify the node asset `id` is unique.
- Effects not applied: ensure a `SkillEffectsRunner` exists in the scene and that the `PlayerSkillHooks` is on the `Player`.
- Points label not updating: confirm `SkillPointsLabel.label` is assigned; events require the object to be active.
- Node assets not discovered: ensure they live under a `Resources` folder; `SkillNodeDatabase` loads via `Resources.LoadAll<SkillNodeDefinition>("")`.
- Unlocks don’t persist: verify `PlayerPrefs` is saving; use `SkillTreeState.ClearAllUnlocks()` during testing.

## Quick checklist

- [ ] Node assets created under `Resources` and ids set.
- [ ] UI nodes have `SkillNodeBinding` with `data` assigned.
- [ ] `SkillEffectsRunner` in scene and `PlayerSkillHooks` on the Player.
- [ ] Skill points increment on level up via `PlayerXP`.
- [ ] Tooltips show descriptions; panel can be toggled open.






