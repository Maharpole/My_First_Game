## 2025-09-07 — Per-weapon fire profiles, hitscan visuals, and aim gizmos

### Overview
- Added `WeaponFireProfile` (per-weapon config) so each weapon defines how it fires even when `ClickShooter` lives on the Player.
- Updated `WeaponEquipper` to auto-apply `WeaponFireProfile` to the Player’s `ClickShooter` on equip, and to wire the weapon `muzzle` from `WeaponGrip`.
- Enhanced `ClickShooter` with hitscan visuals (tracer + impact VFX) and debug gizmos to align muzzle direction with the mouse aim point.

### New/Changed Components

- `WeaponFireProfile` (attach to WEAPON prefabs)
  - Fire mode: `Projectile` or `Hitscan`.
  - Projectile: `bulletPrefab`, `bulletSpeed`.
  - Hitscan: `hitscanDamage`, `hitscanMaxRange`, `hitscanBloomDegrees`, `hitMask`, penetration (`hitscanPenetrate`, `hitscanMaxPenetrations`), knockback (`hitscanKnockback`, `hitscanKnockbackUp`).
  - VFX/SFX: `tracerPrefab`, `tracerLifetime`, `impactVFX`, `fireClips`, `fireVolume`, `firePitchRange`.
  - Method: `ApplyTo(ClickShooter)` used by equip flow.

- `WeaponEquipper`
  - Continues to parent weapon to `rightHand` socket and align using `WeaponGrip.rightHandGrip` when available.
  - Wires `ClickShooter.muzzle` from `WeaponGrip.muzzle` (fallback name search: `Muzzle`, `MuzzlePoint`, `BarrelEnd`, `Tip`).
  - NEW: `ApplyFireProfile(...)` copies settings from equipped weapon to the `ClickShooter` so swapping weapons updates behavior instantly.

- `ClickShooter`
  - Hitscan visuals: optional tracer prefab and impact VFX spawned on hit.
  - Muzzle flash support (auto-finds child named `MuzzleFlash` if not assigned).
  - Debug Gizmos: `drawGizmos`, `gizmosAlways`, `gizmoRayLength`, color fields; draws:
    - Cyan ray: `muzzle.forward`.
    - Orange sphere + line: mouse aim point and line from muzzle.
    - Yellow ray: `faceTarget.forward` when `faceAimDirection` is enabled.

### Wiring Checklist (per-weapon)
1. On weapon prefab, add `WeaponGrip` and assign:
   - `rightHandGrip` (local Z+ down the barrel, Y+ up),
   - `leftHandGrip` (optional for IK),
   - `muzzle` at barrel tip (Z+ out of the barrel).
2. Add `WeaponFireProfile` to the weapon prefab and set mode + parameters, VFX, and sounds.
3. Ensure the Player has `ClickShooter` and `WeaponEquipper` with `rightHand` socket assigned.
4. Enter Play and use `WeaponEquipper → Refresh Weapon Attachments` to test alignment.

### Debug/Calibration Flow
1. Temporarily set `ClickShooter.aimYawOffsetDegrees = 0` and disable `PlayerHandIK` if needed.
2. Turn on `ClickShooter.drawGizmos` (and `gizmosAlways`) to see the cyan muzzle ray and orange aim point.
3. If the cyan ray does not intersect the orange sphere/line:
   - Fix `WeaponGrip.rightHandGrip` axes first (Z+ down barrel, Y+ up).
   - Ensure `muzzle.forward` (Z+) exits the barrel.
   - Rotate the player `rightHand` socket so Z+ aims forward from palm (keep scale 1,1,1).
4. Re-enable IK and introduce small `aimYawOffsetDegrees` if the shouldered stance needs a tiny yaw correction.

### Notes & Compatibility
- FBX axis differences: if the model uses X+ forward, either enable Import “Bake Axis Conversion”, or add a corrective "Pivot" child under the weapon root and place `WeaponGrip` markers under it.
- If a weapon lacks `WeaponFireProfile`, the Player keeps current `ClickShooter` settings (safe default).
- All VFX spawn in world space to avoid parenting artifacts on fast movement.

### Future Work / Open Tasks
- Per-weapon recoil, screenshake, and spread curves (time-based bloom/recoil patterns).
- Aim-down-sights (ADS) mode: FOV/camera blend, sensitivity scaling, shoulder offset.
- Pooling for tracers and impact VFX for GC-free high fire rates.
- Damage types, armor/resistance layers; headshot multipliers via layered hitboxes.
- Decal Projector for bullet marks; optional sparks/debris by surface type.
- Right-hand IK option for extreme weapons; animation additive poses for weapon-specific stances.
- Inventory/tooltip surfacing of weapon stats (mode, damage, range, RPM, bloom).
- Auto-detect and warn if `muzzle`/`rightHandGrip` axes are misaligned (editor validation helper).
- Unit tests/in-editor checks for `WeaponEquipper` alignment and `muzzle` wiring.





