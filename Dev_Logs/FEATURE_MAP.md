POE Auto-Shooter — Feature Map and Dev_Logs Protocol

Machine‑readable snapshot

```json
{
  "project": "POE Auto-Shooter",
  "vision": "Infinitely expandable auto-shooter focused on deep Path-of-Exile-like items, crafting, and talent trees.",
  "current_step_id": "health_and_damage",
  "milestones": [
    {"id": "player_basic_movement", "title": "Player basic movement", "status": "done", "file_refs": ["Assets/Controllers/PlayerController.cs"]},
    {"id": "dev_logs_setup", "title": "Establish Dev_Logs + Feature Map", "status": "done", "file_refs": ["Dev_Logs/FEATURE_MAP.md"]},
    {"id": "camera_follow_zoom", "title": "Camera follow + scroll zoom", "status": "done", "file_refs": ["Assets/Controllers/CameraController.cs"]},
    {"id": "enemies_and_spawner", "title": "Enemy prefab + basic spawner/waves", "status": "todo"},
    {"id": "health_and_damage", "title": "Health system for player/enemies + damage events", "status": "todo"},
    {"id": "autoshooter_core", "title": "Auto-targeting + auto-fire loop (projectiles or hitscan)", "status": "todo"},
    {"id": "loot_coin_pickups", "title": "Enemy death drops coins; player can pick up", "status": "todo"},
    {"id": "coin_system_ui", "title": "Coin counter UI + persistence hook", "status": "todo"},
    {"id": "shop_basic_purchase", "title": "Spend coins to buy items (simple shop UI)", "status": "todo"},
    {"id": "item_system_skeleton", "title": "Item bases, rarity, stats, affix hooks", "status": "todo"},
    {"id": "inventory_and_equipment", "title": "Inventory UI + equip slots; apply stats", "status": "todo"},
    {"id": "item_affixes_generation", "title": "Expandable affix generation + tooltips", "status": "todo"},
    {"id": "save_load", "title": "Save/Load (player, items, coins, tree)", "status": "todo"},
    {"id": "main_menu", "title": "Main menu (Start/Continue/Quit)", "status": "todo"},
    {"id": "pause_menu", "title": "Escape/Pause menu (Resume/Settings/Quit)", "status": "todo"},
    {"id": "talent_tree_scaffolding", "title": "Skill tree data + UI scaffold; node unlocks", "status": "todo"},
    {"id": "skill_node_effects", "title": "Node effects applied to character stats/systems", "status": "todo"},
    {"id": "content_pipeline_expandability", "title": "Data-driven content pipeline (ScriptableObjects dirs)", "status": "todo"},
    {"id": "ux_polish_balance", "title": "Early polish, feedback loops, and balancing pass", "status": "todo"}
  ],
  "agent_update_rules": {
    "edit_this_file_first": true,
    "append_logs_under": "Dev_Logs/",
    "log_naming": "YYYYMMDD-HHMM-short-title.md",
    "required_sections_per_log": ["context", "changes", "rationale", "next_steps"]
  }
}
```

What we are building

- An autoshooter where the player automatically engages enemies; design time is focused on systems depth rather than manual combat complexity.
- Core inspiration from Path of Exile for items, crafting, and passive/talent trees.

Why (goals)

- Prioritize infinitely expandable systems over bespoke skills/moves.
- Make it easy for future contributors/agents to understand the state and continue work safely.

Current state (high level)

- Scene: `Assets/Scenes/SampleScene.unity` with a floor and a controllable character.
- Movement: `Assets/Controllers/PlayerController.cs` moves the character on the X/Z plane.
- Camera: `Assets/Controllers/CameraController.cs` follows the player and supports scroll-wheel zoom (orthographic or perspective).

Immediate next focus

- Build enemies and a simple spawner, then health/damage hooks. Follow with auto-target and auto-fire.

Dev_Logs — purpose and protocol

- Purpose: Provide a compact, non-hallucinatory source of truth for humans and AI. This file is the feature map/index; append detailed work notes as separate dated logs.
- Contents:
  - This Feature Map (you are here): roadmap, current step, and pointers to key files.
  - Chronological logs: one file per discrete change or session with context, changes, rationale, and next steps.
- Update rules (for agents/humans):
  1. Update the machine‑readable JSON block at the top when a milestone status changes.
  2. Add a dated log file under `Dev_Logs/` named `YYYYMMDD-HHMM-short-title.md` describing context/changes/rationale/next steps.
  3. Keep entries small, factual, and reference exact files/functions you touched.
  4. Prefer links to assets by path (e.g., `Assets/Controllers/CameraController.cs`).

Style & conventions

- Be explicit: state assumptions, inputs, outputs, and file paths.
- Keep decisions reversible: document alternatives briefly when appropriate.
- Avoid speculation—record what is, not what might be.

Quick start for a new agent

1. Read the JSON snapshot at the top to know the current step and milestone statuses.
2. Skim this page to understand scope and principles.
3. Make small, safe edits; run the project; then append a dated log with the four required sections.
4. Update the JSON snapshot if you completed or re‑scoped a milestone.

POC development sessions (suggested breakdown)

1) Enemies and Spawner
- Enemy prefab with basic movement/AI toward player
- Spawner that emits waves; simple spawn bounds
- Minimal VFX/SFX placeholders

2) Health and Damage
- `Health` component (max, current, events OnDamaged/OnDeath)
- Player and enemies take damage; death disables entity and fires events

3) Autoshooter Core
- Target acquisition (nearest in range, line-of-sight optional)
- Fire loop with rate, projectile or hitscan, damage from stats

4) Loot & Coin Pickups
- Coins drop on enemy death; magnetized pickup or on-touch
- Coin counter UI element

5) Coin System + Shop (Basic)
- Persisted coin balance in game session
- Simple shop UI to buy a randomly rolled item

6) Item System Skeleton
- ScriptableObject `ItemDefinition` (base type, sockets TBD later), rarity enum
- Common stats pipeline; affix hook points

7) Inventory & Equipment
- Inventory list/grid UI; drag/drop or click-to-equip
- Equipment slots; aggregate equipped stats to a `CharacterStats` component

8) Affixes & Generation
- Affix definitions (ScriptableObject) with stat deltas and tiers
- Item generation rolls base + affixes by rarity; tooltip displays rolled values

9) Save/Load
- Serialize player state (coins, inventory, equipment, stats, skill nodes)
- Load on start/continue; safe file versioning

10) Main Menu
- Start, Continue (if save exists), Quit; basic background and input lock

11) Pause/Escape Menu
- Resume, Settings (placeholder), Quit to menu; pause game time

12) Talent Tree Scaffolding
- Data for nodes (id, position, prerequisites, effect hook id)
- UI to render nodes and edges; unlock/spend points; persistence

13) Skill Node Effects
- Registry mapping node ids to effect functions modifying `CharacterStats` or systems
- Recompute on unlock/load; stack effects safely

14) Content Pipeline (Expandability)
- Directory structure under `Assets/Data/` for Items, Affixes, Enemies, SkillTree
- Authoring rules and validation; addressable/Resources strategy TBD

15) UX Polish & Balance
- Feedback: hit flashes, damage numbers (optional), clearer pickups
- Early balance pass on spawn rates, damage, and item rolls


