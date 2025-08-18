POE Auto-Shooter — Stats Overview

This document summarizes player and item stats supported by the game and how they currently affect gameplay. It is intended as a quick reference while tuning and extending the stat system.

Core stats

- MovementSpeed: Increases or decreases the player’s movement speed.
  - Flat: adds directly to the base speed.
  - Percent: multiplies total speed after flat bonuses.
  - Formula: moveSpeed = (baseMoveSpeed + flat) × (1 + %/100).

- Damage: Increases the player’s AutoShooter damage.
  - Flat: adds to weapon’s base damage.
  - Percent: multiplies after flat.
  - Formula: damage = (baseDamage + flat) × (1 + %/100).

- AttackSpeed: Increases the player’s AutoShooter rate of fire.
  - Percent only: scales fire speed (reduces interval).
  - Formula: fireRate = baseFireRate ÷ (1 + %/100).

- Health: Increases player maximum health.
  - Flat: adds to base max health.
  - Percent: multiplies after flat (and Vitality contribution).
  - Formula: maxHealth = ((baseMaxHealth + flatHealth) + vitality × healthPerVitality) × (1 + %Health/100).

- Vitality: Converts to additional maximum health.
  - Flat points: each point increases max health by healthPerVitality (default 5).

Damage flow (simplified)

- WeaponAttachController spawns an equipped weapon and configures an AutoShooter.
- Player aggregates stats from equipped items and applies:
  - MovementSpeed to player movement and NavMeshAgent.speed.
  - Damage and AttackSpeed to all child AutoShooters.
  - Health, Vitality to player max health.

Enemy stats

- Level (new): enemies can have a level set per prefab/instance.
- Max health scales with level per EnemyHealth settings (see EnemyHealth inspector).
- XP reward scales with enemy level; XP is granted to the Player on enemy death.

Future extensions (suggested)

- Resistances/Armor: reduce incoming damage by flat or percent.
- CriticalChance/CriticalMultiplier: add chance-based damage spikes.
- PickupRadius: increase coin/item pickup range.
- ExperienceGain: percent bonus to experience gained.





