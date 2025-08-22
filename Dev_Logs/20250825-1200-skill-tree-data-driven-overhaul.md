## 2025-08-25 — Data-driven Skill Tree overhaul

- Introduced a scalable skill system aimed at supporting 100+ nodes.
- `SkillNodeDefinition` now distinguishes **Major** vs **Minor** nodes via `nodeType`.
- Nodes declare their graph links by string `parentIds` for easy import/export.
- Added `SkillNodeDatabase` for global lookup of nodes by id.
- Replaced legacy effect switch with generic `StatModifier` structs
  (`StatType` + `ModifierOp` + value) consumed by `PlayerSkillHooks`.
- `SkillEffectsRunner` iterates modifiers and dispatches them to the player.

### Creating a new node
1. Create a `SkillNodeDefinition` asset under `Assets/Resources/SkillNodes`.
2. Set a unique `id`, description, and `nodeType` (Minor/Major).
3. Fill `parentIds` with the ids of prerequisite nodes.
4. For numeric changes, add entries to `statModifiers`:
   - Choose a `StatType` (MaxHealth, ReflectFlat, ReflectPercent, RegenPerSecond).
   - Pick an operation (`Add` or `Multiply`) and a value.
5. For keystones or special behavior, wire functions (e.g., `EnableVelocityScale`) to the `OnApply` UnityEvent.
6. Ensure the node asset resides in a Resources path so `SkillNodeDatabase` can discover it.

### Runtime notes
- `SkillTreeState.ParentsSatisfied` now resolves parents via ids using `SkillNodeDatabase`.
- Existing assets using the old `parents` reference list still work; `parentIds` takes precedence.
- `PlayerSkillHooks.ApplyStatModifier` handles stat updates and logs unhandled stats for easier debugging.

