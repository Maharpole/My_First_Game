## 2025-08-19 — Character Creation, Class Seed, Level-Up Feedback, Skill Points

- Added `CharacterCreation` flow:
  - `MainMenu` now exposes `New Game` → loads `CharacterCreation` scene.
  - `CharacterCreationUI` lets the player pick a starting class (Strength/Dexterity/Intelligence) and enter a name (TMP or legacy InputField). Confirmation saves to `PlayerProfile` and loads `Map_Scene`.
  - `PlayerProfile` persists selected class, name, and unspent skill points.

- Player initialization:
  - `Player` logs `StartingClass` on init for debugging and future skill-tree routing.

- XP / Level system enhancements:
  - `PlayerXP` now grants +1 unspent skill point on each level-up, persists it, and plays feedback.
  - Level-up feedback: optional VFX (`levelUpVFX`) and SFX (`levelUpSFX`, volume) serialized on `PlayerXP` for per-character tuning.

- Portal spawn polish:
  - Added scene spawn framework (`PlayerSpawnPoint`, `SceneSpawnResolver`, `PortalSpawnData`). `Portal` and `PortalSpawner` can target spawn ids; resolver places player accurately on scene load.

- Movement facing polish:
  - `Player` rotates to face movement and dash direction; `ClickToMoveController` aligns rotation with agent velocity (agent.updateRotation=false).

Next: Implement first-level skill tree gate that branches dash functionality and introduces class-specific charges.

### 2025-08-13 — UI hotkey fallback, equipment placeholders, and drag-from-equipment fix

- **Context**: After a power surge, some changes regressed. Restored and hardened UI hotkey, inventory UI, and equipment drag-and-drop behavior.

- **Changes**:
  - **UI toggle hotkey (New Input System fallback)**: `UIToggleHotkey` now auto-binds to `Input_Control.UI.ToggleInventory` if `toggleAction` isn’t wired, so pressing C toggles even without manual scene setup.
  - **Equipment empty placeholders**: `UIEquipmentSlot` guarantees a visible placeholder sprite when empty if `emptyIcon` is not assigned, preventing invisible equipment slots.
  - **Drag from equipment to inventory**: Relaxed the drop condition so equipped items can be dropped into any `UIDropTargetSlot` inventory cell (still requires the cell to be empty by design).

- **Files edited**:
  - `Assets/Scripts/UI/UIToggleHotkey.cs`
  - `Assets/Scripts/UI/UIEquipmentSlot.cs`

- **Operational notes**:
  - Ensure one persistent GameObject in the startup scene has `UIToggleHotkey` with `panels` set to the inventory/equipment UI root.
  - `SimpleInventoryUI` should point to `PlayerInventory.asset`, grid container, and the slot button prefab.
  - Drag-and-drop flows: inventory → equipment, equipment → empty inventory, and equipment ↔ equipment swap supported when types are compatible.

- **Follow-ups**:
  - TODO `test_inventory_drag_drop`: Play-test all drag/drop paths and ring handling. If swap-into-filled-inventory from equipment is desired, add a swap path in `UIDropTargetSlot.AcceptDropItem` or equipment drag logic.


