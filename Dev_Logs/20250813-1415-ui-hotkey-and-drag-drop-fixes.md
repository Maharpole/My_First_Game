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


