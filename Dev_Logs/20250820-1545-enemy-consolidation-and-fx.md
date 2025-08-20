## Enemy consolidation, auto-aggro, and death FX stabilization

Scope: Enemy runtime, mob spawning/modifiers, editor utilities, FX

Key changes
- Consolidated enemy stats/behaviors into `EnemyHealth`:
  - Merged health, contact damage, crit, projectile extras, and movement/aggro.
  - Added movement + chase logic; removed runtime dependency on `EnemyController` logic.
  - Added proximity-based aggro with `aggroRange` and `startAggro`.
  - Uses `Rigidbody.linearVelocity` (Unity 6.2) for motion.
  - Added safe death FX play (world space, no inherited velocity, normalized speed/scale).

- Leveling merged into `EnemyHealth`:
  - Fields: `useLevelScaling`, `level`, `baseMaxHealth`, `healthPerLevel`, `healthLevelMultiplier`.
  - `ApplyLevelToHealth()` computes `maxHealth`; applied on Start unless overridden.
  - XP reward moved in: `baseXP`, `xpPerLevel` (granted on death).
  - Added `ignoreLevelScaling` so spawners can set exact health without double-scaling.

- Spawner and modifiers updated:
  - `PackSpawner` targets `EnemyHealth` and applies `maxHealthOverride`/`moveSpeedOverride`.
  - `MobModifier.ApplyToEnemy(EnemyHealth)`: multiplies move/attack speed, contact damage, and health; adds extra projectiles.

- Editor utility for scene/prefabs cleanup:
  - `Tools > Project > Clean Missing Scripts In Open Scenes` to remove missing script references.
  - `Tools > Project > Clean Missing Scripts In Selected Prefabs` for Project selections.

Death FX issues addressed
- Death particles were moving too fast due to prefab asset references/inherited velocity/local simulation.
- Runtime now:
  - Instantiates particles if the reference is a prefab asset.
  - Forces world simulation space and `simulationSpeed = 1`.
  - Disables Inherit Velocity and Velocity over Lifetime.
  - Detaches and normalizes transform scale.

Migration notes
- Remove `EnemyController` and `EnemyLevel` components from enemy prefabs.
- Configure enemies via `EnemyHealth` only.
- If a `MobType`/spawner overrides health, also set `ignoreLevelScaling = true` on the spawned `EnemyHealth` before writing `maxHealth`.
- Verify `MobModifier` assets (multipliers of 1 = no change).

QA checklist
- Inspector health respected when no spawner override is present.
- With override: health equals override, no level double-application.
- Enemies aggro when within `aggroRange` and chase player using linearVelocity.
- Death FX stays in place at correct speed.
- No missing script references in open scenes/prefabs after running cleanup tools.


