# Dev Log — 2025-08-11 16:30 — Restructure: Inventory/Equipment, Mobs, Coins, Camera, UI

## Context
- Project: POE-like ARPG meets auto-shooter. Focus on deep itemization (affixes/rarity), crafting, passive tree, and replayable combat loops with packs (mobs) and modifiers.
- Goal of this session: clean architecture, replace fragile/overlapping systems, and establish simple, reliable foundations for inventory/equipment UI, enemy packs, currency drops, and hotkey toggling.

## What’s in place now (high level)
- Player
  - `Assets/Scripts/Player/Player.cs`: single owner of movement/health/damage/currency; equipment propagated via `CharacterEquipment`.
  - Click-to-move (`ClickToMoveController`) ignores UI clicks and guards against missing NavMesh.
- Equipment & Items
  - `EquipmentData` (ScriptableObject) drives equipment bases and metadata (slot, weapon flags, 1H/2H, rarity color, optional `weaponProfile`).
  - `CharacterEquipment` enforces slot rules (includes `BodyArmour`, two `Ring` slots, Main/Off Hand with two-hand logic).
   - `UIEquipmentSlot` displays equipped item icons, updates on start and on equipment changed; supports:
     - drag from inventory to equip (returns swapped item to inventory),
     - drag from equipment to inventory (drops to empty cell),
     - swap between equipment slots by dragging.
- Simple Inventory (Minecraft-style)
  - `SimpleInventory` (ScriptableObject): fixed array of `EquipmentData` references editable in the inspector (works in Edit/Play mode).
   - `SimpleInventoryUI`: builds a GridLayout of slot buttons; supports drag from slot to slot (swap), drag from slot to equipment slot (equip). Click-to-equip disabled by design.
- Enemies (mobs/packs)

## 2025-08-19 Skill Tree Integration (Radial Asset)

Implemented a minimal, asset-agnostic skill tree layer that works with the purchased radial node/toggle prefab.

- Skill points
  - Uses existing `PlayerProfile.UnspentSkillPoints` (1 point per level from `PlayerXP`).
  - New `SkillPointsLabel` (`Assets/Scripts/UI/SkillTree2/SkillPointsLabel.cs`) updates a TMP label with "{total}/{spent} skill points".

- Node data and state
  - `SkillNodeData` (ScriptableObject): id, description, parent nodes (pathing), cost (default 1), `OnApply` event.
  - `SkillTreeState` (static): persists unlocked ids in PlayerPrefs, enforces parent requirements, spends 1 point on unlock, raises `OnUnlocked`.

- Node binding
  - `SkillNodeBinding` attaches to the radial Toggle node prefab.
    - Disables Toggle until all parents are unlocked and a point is available.
    - On Toggle ON: calls `SkillTreeState.TryUnlock(data)`, spends 1 point, invokes node `OnApply`, and sets Toggle permanently ON.
    - On hover: shows tooltip using `UITooltip` with node description.

- First skill node effects
  - When unlocked, apply:
    - Velocity affects player size: added `VelocityScaleBySpeed` component (disabled by default); `OnApply` flips `enabledBySkill = true`.
    - +10 base damage reflect: hook a Player method or StatApplier event to add +10 reflect and call `Player.RecomputeAndApplyStats()`.
    - +25 maximum health: add +25 to max health modifier and recompute.
    - +10 health regeneration per second: add +10 regen and recompute (maps to regenPerTick = 10, tickSeconds = 1s).

- Toggle skill tree
  - Use existing `UIToggleHotkey` and bind the tree panel to the UI action (e.g., `UI/ToggleSkillTree`).

Wiring instructions
1) Create `SkillNodeData` assets for each node; set parents to match radial links.
2) On each radial node prefab instance: add `SkillNodeBinding`, assign the node asset and its Toggle.
3) Add `SkillPointsLabel` to a TMP text in the tree header.
4) For the first node, add four `OnApply` handlers (either direct Player methods or a small StatApplier component) to apply reflect/health/regen and enable `VelocityScaleBySpeed` on the player.
5) Bind `UIToggleHotkey` to the tree panel with the desired InputAction.

Notes
- Pathing is enforced strictly by the `parents` list on each node data asset.
- Spent points are computed from unlocked count (cost=1 assumed).
  - Data-driven scaffolding with `EnemyType`, `MobType`, `MobModifier`, `SpawnTable`, `MapConfig`.
  - Runtime `MobDirector` places packs with spacing; `PackSpawner` projects spawns to ground/NavMesh.
  - `AggroGroup` keeps packs idle; aggro triggers by proximity or damage. `EnemyController` has aggro flag to avoid auto-chase on start.
- Currency (coins)
  - Consolidated: `CoinDropper` now spawns coins directly (no centralized spawner). `CoinManager` handles UI/total. `Coin` handles pickup and optional magnet behavior.
- Weapons (auto-shooter foundation)
  - `WeaponData` + `AutoShooter` (targets nearest enemy in range; configurable damage/fire rate/bullets/spread).
  - `EquipmentData` now has `weaponProfile : WeaponData` for weapon items.
  - `WeaponAttachController` on `Player` listens to `CharacterEquipment.onEquipmentChanged` and:
    - Spawns an `EquippedWeapon` under an attach point when a MainHand weapon is equipped.
    - Ensures an `AutoShooter` and configures it from `weaponProfile` (or Resources fallback `WeaponProfiles/DefaultWeapon`).
  - `WeaponManager` remains (legacy orbit/placement); current flow uses `WeaponAttachController`.
- Camera/UI
  - `CameraController`: simple follow with offset/zoom.
  - `UIToggleHotkey`: action-based if new Input System is enabled; fallback to legacy key with conditional compilation.

## Major refactors and deletions
- Removed overlapping/legacy player data and health managers; `Player.cs` now delegates equipment to `CharacterEquipment`.
- Replaced tetris inventory system:
  - Deleted: `InventoryGrid.cs`, `InventoryGridUI.cs`, `InventoryItemInstance.cs`, `EquipmentSlotDropTarget.cs`, and test helpers tied to the old grid.
  - Added: `SimpleInventory.cs`, `SimpleInventoryUI.cs`, `UIEquipmentSlot.cs` with drag/drop between inventory cells and equipment slots.
- Rings consolidated:
  - `EquipmentType.RingLeft/RingRight` replaced by single `EquipmentType.Ring` plus two ring slots (`ring1`, `ring2`).
  - Added `EquipmentType.BodyArmour`.
- Coins: inlined spawn in `CoinDropper`, removed `CoinSpawner` and references.
- Packs: `EnemyController` now starts idle unless aggro’d; `AggroGroup` controls when to alert; `PackSpawner` projects spawns to ground/NavMesh to fix under-ground issues after ground scale changes.

## Current file map (key code)
- Player: `Assets/Scripts/Player/Player.cs`, `Assets/Scripts/Player/ClickToMoveController.cs`
- Equipment Core: `Assets/Scripts/Equipment/EquipmentData.cs`, `EquipmentType.cs`, `EquipmentSlot.cs`, `CharacterEquipment.cs`
- Inventory UI: `Assets/Scripts/Inventory/SimpleInventory.cs`, `Assets/Scripts/UI/SimpleInventoryUI.cs`, `Assets/Scripts/UI/UIEquipmentSlot.cs`
- Weapons: `Assets/Scripts/Weapon/WeaponData.cs`, `Assets/Scripts/Weapon/AutoShooter.cs`, `Assets/Scripts/Weapon/Bullet.cs`, `Assets/Scripts/Weapon/WeaponManager.cs`
- Mobs: `Assets/Scripts/Mobs/*.cs`
- Currency: `Assets/Scripts/Currency/*.cs`
- UI Utility: `Assets/Scripts/UI/UIToggleHotkey.cs`, `Assets/Scripts/Camera/CameraController.cs`

## How to use (dev quickstart)
- Inventory/Equipment:
  1) Create `SimpleInventory` asset and populate with `EquipmentData` items.
  2) In scene UI, add `SimpleInventoryUI`:
     - Assign `inventoryData`, `gridParent` (with GridLayoutGroup), `slotButtonPrefab` (Button + Image child), and `equipment`.
  3) For each equipment panel (Helmet, BodyArmour, Ring 1/2, Main/Off Hand), add `UIEquipmentSlot`:
     - Set `slotType`, `useSecondRing` for the 2nd ring, assign `equipment`, `inventoryData`, `icon`.
  4) Drag from inventory → equipment slot to equip; old item returns to the inventory; icons update automatically.
- Packs:
  1) Author `EnemyType`, `MobType`, `MobModifier`, `SpawnTable`, `MapConfig`.
  2) Place `MobDirector` in scene, assign the above, and call `Generate()` (via a small bootstrap or manually).
- Weapons (current): equip a MainHand weapon item (isWeapon=true) to reflect in character build; auto-attach controller WIP (see next steps).
- Hotkey:
  - Add `UIToggleHotkey` to toggle CharacterScreen with C (legacy) or bind an InputAction (new system).

## Known limitations / open tasks
- Weapons attachment to equipment flow (WIP):
  - Need a small `WeaponAttachController` that listens to `CharacterEquipment.onEquipmentChanged`, spawns/destroys a `WeaponData.weaponPrefab` (with `AutoShooter`) when MainHand changes, and assigns `AutoShooter.weaponData`. This cleanly bridges equipment → auto-shooter.
  - Optional: add a `weaponProfile` field to `EquipmentData` to directly reference the `WeaponData` asset (author-friendly mapping). Currently suggested in plan but not added to the class yet.
 - Equipment→Inventory drag: Implemented. Dragging an equipped item over an empty inventory cell unequips and places it there.
- Tooltips: add hover tooltips (name, rarity color, rolled stats) for both inventory slots and equipment slots.
- Save/Load: persist `SimpleInventory` slots and `CharacterEquipment` state (and coins).
- Affix system integration: we implemented affix definitions, db, generator, and item levels with tier gates earlier; connect generated affixes to stats pipeline when equipping items.
- Stats pipeline: aggregate `CharacterEquipment.GetAllStatModifiers()` into Player stats (damage, attack speed, defenses) and pass into `AutoShooter` and other systems.
 - Input System migration: currently `UIToggleHotkey` supports both; consider moving all input to the new Input System (separate gameplay vs UI action maps). We temporarily removed the generated Input Actions asset due to GUID issues; legacy input path is active.
- Performance: object pooling for bullets/enemy/projectile VFX; distance-based activation for mobs.
- Content pipeline: consistent folders/naming, authoring docs for Items, Affixes, Enemies, Packs.

## Rationale (why these choices)
- ScriptableObjects for data: safe authoring without play mode, minimizes code coupling, supports scalable content.
- Simple fixed-slot inventory: fast to implement, easy to test, avoids tetris complexity until core loop is stable.
- Decoupled equipment vs weapons: `CharacterEquipment` owns what’s equipped; a separate controller attaches/detaches visual/logic prefabs (easier to test and swap implementations).
- Mob Director + Pack Spawner: procedural packs with spacing behave like ARPG zones; aggro group keeps packs idle until engaged, improving feel/perf.
- Conditional input: `UIToggleHotkey` works without forcing the new Input System.

## Next steps (actionable)
1) Implement `WeaponAttachController` on Player:
   - On equipment changed, if MainHand weapon present, spawn its `WeaponData.weaponPrefab`, set `AutoShooter.weaponData` and stats. Destroy previous instance.
   - Add optional `weaponProfile` to `EquipmentData` and use directly.
2) Equipment→Inventory drag (symmetry):
   - Add a `UIDragSource` component to the equipment slot icon that, on drop over an empty inventory cell (UIDropTargetSlot), unequips and places the item there.
3) Tooltips: hover UI to show `EquipmentData.GetTooltipText()`.
4) Save/Load: serialise `SimpleInventory.slots` and `CharacterEquipment` (by asset GUID or IDs).
5) Stats: compute from `GetAllStatModifiers()` and apply to Player/AutoShooter (damage, fire rate, etc.).
6) Weapon upgrades: re-enable upgrade application in `WeaponManager.ApplyUpgrades` from a new per-player purchased-upgrades store.
7) Polish: pooling for bullets, input map separation, option to pause gameplay when inventory is open, label toggle for dropped items.

---
If you need any examples or want a ready prefab for CharacterScreen (grid + equipment), I can supply one quickly. This log should give the next agent full context on what exists, what was changed, and what to build next.
