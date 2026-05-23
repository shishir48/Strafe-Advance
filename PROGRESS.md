# Strafe Advance — Session Progress

## Project
- **Path:** `/Users/shishirsingh/Strafe-Advance`
- **GitHub:** https://github.com/shishir48/Strafe-Advance
- **Engine:** Unity 6 (6000.4.7f1), URP, Android target
- **APK:** `/Users/shishirsingh/StrafeAdvance.apk` (last built 2026-05-22, Mono2x)
- **Tests:** **65 passing / 0 failing** (EditMode)
- **Branch:** main (all committed)

---

## Where to resume next session

Open `claude` in the project dir. To get back into a runnable state:

```
StrafAdvance/1. Add Enemy Layer
StrafAdvance/2. Create ScriptableObject Assets
StrafAdvance/3. Create Prefabs
StrafAdvance/4. Setup GameScene
StrafAdvance/9. Apply Kenney 3D Models
StrafAdvance/10. Apply Sci-Fi Upgrade
StrafAdvance/11. Bootstrap Addressables
StrafAdvance/12. Add HitReact To Enemies
```

Then hit Play. Auto-starts after 3s (editor) or tap (device). All P1+P2+P4+P5+P6 systems wired by `Setup GameScene`.

---

## Backlog priorities (next session)

Highest impact remaining items, picked by ROI:

1. **Phase 3 — Mixamo rig + 3-4 player animations** (needs Adobe login on user side)
2. **Phase 4 — Main menu + loadout screen** (currently no menu; goes straight to game)
3. **Phase 5 — Wire actual SFX audio clips** to AudioManager `sounds[]` list (SfxRouter calls PlaySFX but clips are empty)
4. **Phase 6 — Shop screen** to spend soft currency on weapons/perks/skins
5. **Phase 7 — CI/CD GitHub Actions** + crash reporting
6. **Leftover P2** — slide movement, aim-assist, unified EnemyBrain (low value)

---

## Done — Phase 1 (Foundation Refactor) ✅

| # | Item | Outcome |
|---|------|---------|
| P1.1 | Fix 4 pre-existing test failures | 36/36 green baseline |
| P1.2 | SaveSystem AES + JSON + atomic + versioned | `Core/SaveSystem.cs` + 4 tests |
| P1.3 | Mixed-type WaveConfig (`WaveEntry[]`) | back-compat with legacy + 2 tests |
| P1.4 | New Input System (`GameInput` facade) | all `UnityEngine.Input` gone |
| P1.5 | Addressables migration | `AssetLoader` + menu 11 + 9 keys |
| P1.6 | VContainer DI (`GameLifetimeScope`) | 5 services registered |
| P1.7 | EventBus + StateMachine | GameManager FSM-driven, +9 tests |

## Done — Phase 2 (Gameplay Depth) — 22 items ✅

| # | Item | Outcome |
|---|------|---------|
| P2.1 | Damage numbers | Pooled TMP, white normal / gold crit |
| P2.2 | Screen shake | Perlin trauma model, hooks EnemyKilled/PlayerDamaged |
| P2.3 | Hitstop | 0.04s grunt / 0.10s elite / 0.06s player hit |
| P2.4 | Combo + multiplier | ×1→×2 at 5, ×4 at 10, ×8 at 20 + 5 tests |
| P2.5 | Mixed waves L1 | W4/W7/W9 first conversion to entries |
| P2.6 | Charger enemy | `EnemyType.Charger` lateral homing melee |
| P2.7 | PowerUpDropper | type-based drop chance, hooks `EnemyKilled` |
| P2.8 | XP/level/perks | `PlayerProgression` + 5-perk catalog + 4 tests |
| P2.9 | Dodge roll | 0.25s dash + 1.5s cooldown + i-frames |
| P2.10 | Sniper enemy | telegraphed laser sight + homing shot |
| P2.11 | Weapon system | 5 weapons + perk-stat layering + 4 tests |
| P2.12 | Shielded enemy | front-cone block, breaks after 5 chips |
| P2.13 | Splitter enemy | 3 mini-grunt fragments on death |
| P2.14 | Perk equip UI | runtime panel, auto-open on level-up |
| P2.15 | Drone swarm | boids: cohesion+separation+advance+homing |
| P2.16 | EnemyHitReact | universal flash + scale-pop + knockback |
| P2.18 | Charger telegraph | Approach→WindUp→Lunge FSM |
| P2.19 | Mini-Boss | HP bar + 2 phases + shake on transition |
| P2.20 | Aim leading + difficulty | `WithDifficulty()` scales HP/dmg by player level |
| P2.21 | Sprint + stamina | 1.6× speed, 5s stamina, regen after 1s |
| P2.22 | Ragdoll-lite death | physics tumble + fade + auto-destroy skip |
| P2.23 | KillCam | slow-mo 0.28× + camera zoom on MiniBoss/Boss death |

## Done — Phase 4/5/6 essentials — 4 items ✅

| # | Item | Outcome |
|---|------|---------|
| P4.1 | ModernHUD | top-left HP+stamina+dodge pip, top-center wave+combo, top-right rolling score |
| P4.2 | PauseMenu | Esc/Start toggle, Resume/Perks/Restart/Quit, freezes time |
| P5.1 | SfxRouter | EventBus→AudioManager bridge, 7 new SoundIDs (Dodge/ShieldHit/ComboTier/PerkUnlock/UIClick/UIConfirm/EliteDeath) |
| P6.1 | CurrencyService + RunSummary | soft-currency drops per enemy type + post-run screen on win/lose |

## Skipped (low ROI)
- P2.17 unified EnemyBrain — local FSMs (Charger/MiniBoss) cover the cases that needed it
- Aim assist for controller — defer to controller QA pass

---

## Architecture (where things live)

```
Assets/_Game/Scripts/
├── Core/
│   ├── AssetLoader.cs           — Addressables → Resources fallback
│   ├── ComboTracker.cs          — kill streak + multiplier
│   ├── CurrencyService.cs       — (NEW) soft-currency on kill + persist
│   ├── DifficultyService.cs     — per-level multiplier
│   ├── EventBus.cs              — typed pub/sub
│   ├── GameInput.cs             — Input System facade
│   ├── GameLifetimeScope.cs     — VContainer DI scope
│   ├── GameManager.cs           — FSM-driven state + InitFlow
│   ├── KillCam.cs               — boss-death slow-mo + zoom
│   ├── SaveData.cs              — schema (versioned)
│   ├── SaveSystem.cs            — AES + atomic + migrate
│   └── StateMachine.cs          — generic FSM
├── Combat/
│   ├── AutoShooter.cs           — reads equipped weapon + perks
│   ├── Bullet.cs                — pooled, layer + IDamageable detection
│   ├── DamageNumber.cs          + DamageNumberSpawner.cs
│   ├── Hitstop.cs               — Time.timeScale freeze
│   ├── PowerUp.cs               + PowerUpDropper.cs
│   ├── ScreenShake.cs           — Perlin trauma model
│   └── WeaponConfig.cs          + WeaponCatalog (5 weapons)
├── Enemies/
│   ├── EnemyBase.cs             — virtual TakeDamage + SuppressAutoDestroy
│   ├── EnemyConfig.cs           — WithDifficulty + aimLead + jitter
│   ├── EnemyHitReact.cs         — flash + pop + knock (universal)
│   ├── EnemyRagdoll.cs          — tumble physics + fade
│   ├── ChargerEnemy.cs          — Approach/WindUp/Lunge FSM
│   ├── DroneEnemy.cs            — boids flocking
│   ├── MiniBossEnemy.cs         — HP bar + 2 phases
│   ├── ShieldedEnemy.cs         — front-cone block
│   ├── SniperEnemy.cs           — laser telegraph + homing
│   ├── SplitterEnemy.cs         — fragment spawner
│   ├── GruntEnemy.cs            — aim-leading bullet
│   ├── FlankerEnemy.cs / EliteEnemy.cs / BossController.cs
├── Player/
│   ├── PlayerController.cs      — strafe + dodge + sprint
│   ├── PlayerHealth.cs          — i-frames
│   ├── AutoShooter.cs (Combat ref) — weapon-driven
│   └── PlayerBuffs.cs
├── Progression/
│   ├── Perk.cs                  + PerkCatalog (5)
│   └── PlayerProgression.cs     — XP + level + unlocks
├── Audio/
│   ├── AudioManager.cs          — singleton + SFX pool
│   ├── SfxRouter.cs             — (NEW) EventBus→AudioManager bridge
│   └── SoundID.cs               — 14 sound IDs
├── UI/
│   ├── HUDController.cs         — legacy (still wired)
│   ├── ModernHUD.cs             — (NEW) production HUD
│   ├── PauseMenu.cs             — (NEW)
│   ├── PerkEquipPanel.cs        — level-up perk picker
│   ├── RunSummaryPanel.cs       — (NEW) post-run screen
│   ├── MainMenuController.cs / LevelSelectController.cs (legacy)
│   ├── GameOverController.cs / LevelCompleteController.cs (legacy)
│   └── ShopController.cs (placeholder)
├── Level/
│   ├── WaveConfig.cs            — entries[] mixed-type
│   ├── WaveSpawner.cs           — 9 enemy types + difficulty scaling
│   └── LevelConfig.cs
├── Editor/
│   ├── GameSetup.cs             — all menu items 1-12
│   ├── AddressablesSetup.cs     — bootstrap Addressables
│   └── BatchBuilder.cs          — APK builder
└── _Game.Scripts.asmdef         — Unity.InputSystem, Unity.Addressables, Unity.ResourceManager, VContainer
```

---

## StrafAdvance Menu Items

| # | Item | Action |
|---|------|--------|
|   | Play Game | Toggle play mode |
| 1 | Add Enemy Layer | Tag + Layer setup |
| 2 | Create ScriptableObject Assets | 10 enemy configs + 3 levels × 10 waves |
| 3 | Create Prefabs | All enemy + bullet + powerup prefabs |
| 4 | Setup GameScene | Full scene rebuild — singletons, DI scope, HUD, pause, killcam, sfxrouter, currency, runsummary |
| 5 | Setup Bootstrap Scene | Build settings |
| 6 | Create Materials | Basic mat assignment |
| 7 | Wire HUD | Legacy HUD wiring |
| 8 | Upgrade Graphics (Sci-Fi Neon) | Legacy (superseded by 10) |
| 9 | Apply Kenney 3D Models | FBX swap + blaster attach |
| 10 | Apply Sci-Fi Upgrade | Materials + post-fx + VFX + corridor + bullet trail |
| 11 | Bootstrap Addressables | Register Resources/ as Addressables |
| 12 | Add HitReact To Enemies | Retrofit HitReact + Ragdoll on every enemy prefab |
|   | Build Android APK | Mono2x APK via BuildPipeline |
|   | Rewire Player Prefab | Re-assign serialized refs |

---

## What works ✅

- **10 waves × 3 levels** with mixed-type entries; L1 uses 9 enemy types
- **Enemies**: 9 types + boss, each with telegraphs, hit reactions, ragdoll deaths, difficulty scaling
- **Player**: strafe + dodge (i-frames) + sprint (stamina) + 5 weapons + 5 perks
- **Combat juice**: damage numbers, screen shake, hitstop, kill cam, ragdolls
- **Progression**: XP per kill → level → perk unlock → equip via panel → live AutoShooter refresh
- **Currency**: soft-currency drops per enemy type, persists, run summary screen
- **HUD**: HP + stamina + dodge pip + wave + combo + score (rolling tween)
- **Audio routing**: SfxRouter bridges all gameplay events to AudioManager.PlaySFX (clips empty — needs SFX asset wiring)
- **Pause**: Esc/Start opens menu, freezes time, Resume/Perks/Restart/Quit
- **Save**: AES JSON atomic with backup rotation + schema versioning
- **DI**: GameLifetimeScope registers 5 services
- **Tests**: 65/65 EditMode pass

## Known issues / TODO
- AudioManager `sounds[]` empty — SFX routes fire but play nothing. Drop in AudioClips next.
- Main menu / loadout screen still legacy stubs
- Coplay MCP requires new claude session to attach
- No PlayMode tests yet (Phase 7)
- Run summary score "XP earned" is `score / 10` — derive properly when reward economy is finalized

---

## Test suite

```
Assets/_Game/Tests/EditMode/
├── BossControllerTests.cs
├── ComboTrackerTests.cs            (P2.4)
├── DamageSystemTests.cs
├── EnemyBaseTests.cs
├── EventBusTests.cs                (P1.7)
├── ObjectPoolTests.cs
├── PlayerHealthTests.cs
├── PlayerProgressionTests.cs       (P2.8)
├── SaveSystemTests.cs              (P1.2)
├── ScoreCalculatorTests.cs
├── StateMachineTests.cs            (P1.7)
├── UnlockRegistryTests.cs
├── WaveSpawnerTests.cs
└── WeaponCatalogTests.cs           (P2.11)
```

Run via `mcp__mcp-for-unity__run_tests` or Unity Test Runner.

---

## Recent commits (most recent first)

```
5c93604d feat(P4.1-P6.1): ModernHUD + PauseMenu + SfxRouter + CurrencyService + RunSummaryPanel
b6e3f89f docs: log P2.21-P2.23
70ab422c feat(P2.21-P2.23): sprint+stamina + EnemyRagdoll + KillCam
41e3d247 docs: log P2.15-P2.20 enemy overhaul
46bf6fee feat(P2.15-P2.20): senior enemy overhaul — HitReact, Charger telegraph, Drone swarm, MiniBoss, aim-leading, difficulty
bfc67f48 docs: log P2.12-P2.14
8b5de7d1 feat(P2.12-P2.14): Shielded + Splitter + PerkEquipPanel
3850e199 docs: log P2.9-P2.11
8b16b67b feat(P2.9-P2.11): dodge roll + sniper enemy + WeaponConfig catalog
ae8396be docs: log P2.6-P2.8
1811a640 feat(P2.7-P2.8): PowerUpDropper + PlayerProgression
caa1bfd7 feat(P2.6): EnemyType.Charger
eacbe452 docs: log P2.1-P2.5
82069293 feat(P2.1-P2.5): combat juice batch
af5c333b docs: mark Phase 1 complete
d5cf31db feat(P1.7): EventBus + StateMachine
59cdd5d6 feat(P1.5-P1.6): Addressables + VContainer DI
a6e553c5 feat(P1): roadmap + SaveSystem + WaveEntry + InputSystem
```
