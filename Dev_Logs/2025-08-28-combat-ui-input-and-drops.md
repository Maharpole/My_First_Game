## 2025-08-28 — Combat telegraphs, input/UI polish, item drops, XP table

### Player & Weapons
- Replaced AutoShooter with ClickShooter as primary firing path.
  - Aims at mouse, rotates player (optional), fires bullets toward cursor.
  - Added muzzle flash support (auto-find child "MuzzleFlash") and randomized fire SFX (clip list + pitch range).
- WeaponAttachController now routes weapon muzzle (Muzzle/MuzzlePoint/BarrelEnd/Tip) to `ClickShooter.muzzle` and supports:
  - Right/left hand sockets, dual wield stance flagging, alignment transforms for exact hand placement.
  - Live preview edits in Edit Mode; cleans up preview instances.

### Player feedback
- Player damage feedback:
  - Randomized damage hit sounds (clip list + pitch range) and non-instantiating hit particles (reuses child system).

### Enemy AI & Telegraphs
- Added telegraphed melee swipe attacks:
  - Enemy halts within `attackRange`, shows a ground-aligned sector indicator that fills during `attackWindup`, then strikes.
  - Indicator spawns at ground via long raycast using `telegraphGroundMask`, lifted slightly to avoid z-fighting, and destroyed after hit.
  - Holds position while attacking; resumes after `attackCooldown`.
  - Standoff behavior: NavMeshAgent stoppingDistance ≈ 0.9 × `attackRange` (braking on), RB fallback zeroes velocity at standoff.
- Hit indicator implementation:
  - New unlit SDF sector shader (`Game/HitIndicatorSector`) + `HitIndicator` component (radius/angle/progress via MPB).
  - Animated inner progress during windup; oriented to enemy facing.
- EnemyAnimatorBridge: added `IsAttacking` bool setter; EnemyHealth toggles it during windup/strike.

### Inventory & Item drops
- Dragging an item out of inventory now drops it to the ground at player feet:
  - `SimpleInventoryUI.itemPickupPrefab` (assign `Item_Pickup_Prefab`).
  - Raycasts to ground, instantiates pickup, populates `RuntimeEquipmentItem.generated.baseEquipment`, clears slot.
  - Overlay pickup UI remains in sync.

### UI/UX
- DraggableWindow improvements:
  - Close button support; fit-to-screen scaling with padding; robust clamping (accounts for Canvas scaling).
  - Input blocking while pressing/dragging handle so ClickShooter won’t fire.
- Escape management: close open windows on first Esc; toggle Pause (Time.timeScale) if none are open.

### Combat & projectiles
- Bullet: sweep cast detects triggers; supports `EnemyHitboxProxy` for oversized hit volumes; knockback retained.

### XP progression
- PlayerXP now uses a 1–100 XP table (Total/To Gain provided) for `xpToNextLevel`; caps at 100 (xpToNext=0).
- EnemyLevel: optional quadratic XP reward (`a*l^2 + b*l + c`, clamped with floor) for future scaling; falls back to linear when off.

### Known/notes
- World-aligned decals recommended (URP Decal Projector) if indicators must conform perfectly to steep/uneven terrain.
- Ensure ground layer is included in `telegraphGroundMask` for accurate placement.


