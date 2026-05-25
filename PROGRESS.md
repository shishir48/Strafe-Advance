# Strafe Advance — Session Progress

## Project
- **Path:** `/Users/shishirsingh/Strafe-Advance`
- **GitHub:** https://github.com/shishir48/Strafe-Advance
- **Engine:** Unity 6 (6000.4.7f1), URP, Android target
- **APK:** `/Users/shishirsingh/StrafeAdvance.apk` (last built 2026-05-22, Mono2x)
- **Tests:** **105 EditMode + 6 PlayMode = 111 passing / 0 failing** (CI matrix runs both on PR/push)
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
StrafAdvance/13. Setup Main Menu          ← (NEW — additive for existing scenes)
```

Then hit Play. Game now boots into **Main Menu** (`MainHubController`) → Play → Loadout → Start Run. The auto-start fallback only kicks in if MainHub is missing. All P1+P2+P4+P5+P6 systems wired by `Setup GameScene`.

---

## Backlog priorities (next session)

Highest impact remaining items, picked by ROI:

1. **Phase 3 — Mixamo rig + 3-4 player animations** (needs Adobe login on user side)
2. **Phase 5 — Wire actual SFX audio clips** to AudioManager `sounds[]` list (SfxRouter calls PlaySFX but clips are empty)
3. **Phase 4 — Localization** (SmartLocalization, EN+ES+JP+ZH-CN)
4. **Phase 6 — Battle Pass + daily login + leaderboards**
5. **Phase 7 — Crash reporting** (Crashlytics or Sentry)
6. **Phase 7 — Performance profiling pass** (SRP Batcher, GPU Instancing, LOD groups, occlusion)
7. **Leftover P2** — slide movement, aim-assist, unified EnemyBrain (low value)

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

## Done — Phase 4/5/6/7 essentials — 19 items ✅

| # | Item | Outcome |
|---|------|---------|
| P4.1 | ModernHUD | top-left HP+stamina+dodge pip, top-center wave+combo, top-right rolling score |
| P4.2 | PauseMenu | Esc/Start toggle, Resume/Perks/Restart/Quit, freezes time |
| P4.3 | MainHubController | runtime-built front door — animated title, currency chip, Play/Loadout/Shop/Settings/Quit; auto-shown when `GameState=Menu` |
| P4.3 | LoadoutPanel | weapon picker from unlocked catalog + equipped-perks display + Start Run; persists `equippedWeaponId` and live-refreshes AutoShooter |
| P4.5 | SettingsPanel | sliders (Music/SFX/UI/Sensitivity) + toggles (Vibration/InvertY/Colorblind) + Quality dropdown + Reset Profile / Reset Tutorial; persists `SaveData.settings` and applies to AudioManager + QualitySettings live |
| P4.6 | TutorialController | first-run 4-step overlay (Strafe→Sprint→Dodge→Combo). FSM-driven, advances on action detection, persists `profile.tutorialCompleted`. Skip button. Reset via Settings |
| P5.1 | SfxRouter | EventBus→AudioManager bridge, 7 new SoundIDs (Dodge/ShieldHit/ComboTier/PerkUnlock/UIClick/UIConfirm/EliteDeath) |
| P6.1 | CurrencyService + RunSummary | soft-currency drops per enemy type + post-run screen on win/lose |
| P6.2 | Shop w/ soft currency | Tabbed (Weapons/Cosmetics) shop; weapons use `CurrencyService.TrySpend`; cosmetics keep IAP path; Equip/Buy/Locked states reactive to balance |
| P6.3 | DailyLoginService | UTC-day streak tracking, escalating reward curve (50→500 caps at day 7), gap resets to 1, idempotent same-day check-in, emits `DailyLoginCheckedIn` event |
| P6.4 | Achievements (8 catalog) | Predicate-based unlocks (kill counts, level, win, wave, streak); grants currency on unlock, retroactive eligibility, emits `AchievementUnlocked` |
| P6.4 | ToastNotifier | Queued slide-in cards at bottom-center, listens to AchievementUnlocked + DailyLoginCheckedIn, non-blocking (no raycast capture) |
| P7.1 | CI: EditMode tests | `.github/workflows/tests.yml` — Unity Test Runner on PR + push to main via `game-ci/unity-test-runner@v4`, Library cached |
| P7.1 | CI: Android APK build | `.github/workflows/build-android.yml` — Android build on `v*` tag via `game-ci/unity-builder@v4`, APK attached to GitHub release + uploaded as artifact |
| P7.2 | CrashReporter | In-process unhandled-exception capture, 50-entry breadcrumb ring, atomic crash-report.json persistence, pluggable `ICrashUploader` (default no-op, Sentry/Crashlytics adapter slots in via `SetUploader`); auto-breadcrumbs state changes + waves + damage |
| P6.5 | BattlePassService + Panel | 10-tier Season 1 with linear XP curve. Per-lane claim state (free vs premium) so retroactive premium unlock works. UI: scrollable tier list, XP-to-next bar, Unlock Premium CTA (500 credits). MainHub button + top-right tier chip + ToastNotifier tier-up popup |
| P7.4 | PlayMode smoke tests | `GameSceneSmokeTests` loads the actual scene, asserts boot is error-free, reflects all 19 WaveSpawner prefab/config slots to assert none are null. Catches scene-wiring regressions EditMode tests can't (e.g. the wave-3 dead-lock from missing Drone prefab). CI matrix runs EditMode + PlayMode in parallel |
| P7.4 | PlayMode integration tests | `GameplayIntegrationTests` drives `EventBus<EnemyKilled>` publishes against the live scene + asserts BattlePass XP grows, CurrencyService grants the per-type drop, CurrencyPopupSpawner spawns a visible popup, all core singletons (Player/HUD/MainHub/BP) are wired. Validates the event-routing graph end-to-end |
| P6.6 | Currency drop popups | `CurrencyPopup` + `CurrencyPopupSpawner`: pooled world-space "+N ◆" floating text at every enemy death (uses new `EnemyKilled.WorldPos` field). Billboarded to main camera, fade-out over 0.85s. Defensive lazy-init handles inactive-Instantiate Awake-delay |
| P4.7 | PauseMenu Settings button | Pause menu now has Resume/Perks/Settings/Restart/Quit (5 buttons). Settings opens `SettingsPanel` without leaving pause — closing returns to the still-paused menu |

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
│   ├── CrashReporter.cs         — (NEW P7.2) breadcrumb ring + crash persistence + pluggable upload
│   ├── DifficultyService.cs     — per-level multiplier
│   ├── EventBus.cs              — typed pub/sub
│   ├── GameInput.cs             — Input System facade
│   ├── GameLifetimeScope.cs     — VContainer DI scope
│   ├── GameManager.cs           — FSM-driven state + BeginRunFromMenu
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
│   ├── Achievement.cs           — Achievement struct + 8-entry catalog
│   ├── AchievementService.cs    — predicate-driven re-evaluator
│   ├── BattlePass.cs            — (NEW P6.5) BattlePassReward / BattlePassTier / BattlePassCatalog (10-tier Season 1)
│   ├── BattlePassService.cs     — (NEW P6.5) XP tracking + per-lane claim + UnlockPremium
│   ├── CurrencyService.cs       — soft-currency on kill + persist + TrySpend/Grant
│   ├── DailyLoginService.cs     — UTC streak + reward curve
│   ├── Perk.cs                  + PerkCatalog (5)
│   └── PlayerProgression.cs     — XP + level + unlocks
├── Audio/
│   ├── AudioManager.cs          — singleton + SFX pool
│   ├── SfxRouter.cs             — (NEW) EventBus→AudioManager bridge
│   └── SoundID.cs               — 14 sound IDs
├── UI/
│   ├── HUDController.cs         — legacy (still wired)
│   ├── ModernHUD.cs             — production HUD
│   ├── PauseMenu.cs             — pause overlay
│   ├── MainHubController.cs     — (NEW) front-door menu (Play/Loadout/Shop/Settings/Quit)
│   ├── LoadoutPanel.cs          — (NEW) pre-run weapon picker + perk display
│   ├── ShopController.cs        — (REWRITE) tabbed shop (Weapons via currency / Cosmetics via IAP)
│   ├── SettingsPanel.cs         — (NEW) audio + sensitivity + toggles + quality + reset
│   ├── PerkEquipPanel.cs        — level-up perk picker
│   ├── RunSummaryPanel.cs       — post-run screen
│   ├── ToastNotifier.cs         — (NEW P6.4) queued bottom-center popups for achievements + daily login
│   ├── TutorialController.cs    — first-run 4-step overlay
│   ├── BattlePassPanel.cs       — (NEW P6.5) scrollable tier list, claim buttons, premium CTA
│   ├── MainMenuController.cs / LevelSelectController.cs (legacy)
│   └── GameOverController.cs / LevelCompleteController.cs (legacy)
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
| 13 | Setup Main Menu | Additive — drop MainHub/Loadout/Shop/Settings + Tutorial + Crash/Daily/Achievement/Toast + BP singletons into the active scene without rebuilding |
| 14 | Sync All Singletons | (NEW) Additive sync of every service GameObject (use after pulling new singletons — broader than menu 13) |
| 15 | Rewire WaveSpawner Prefabs | Additive fix for the wave-3 dead-lock — re-runs the full prefab+config SetField pass on the existing WaveSpawner in the active scene |
|   | Build Android APK | Mono2x APK via BuildPipeline |
|   | Rewire Player Prefab | Re-assign serialized refs |

---

## What works ✅

- **10 waves × 3 levels** with mixed-type entries; L1 uses 9 enemy types
- **Enemies**: 9 types + boss, each with telegraphs, hit reactions, ragdoll deaths, difficulty scaling
- **Player**: strafe + dodge (i-frames) + sprint (stamina) + 5 weapons + 5 perks
- **Combat juice**: damage numbers, screen shake, hitstop, kill cam, ragdolls
- **Progression**: XP per kill → level → perk unlock → equip via panel → live AutoShooter refresh
- **Currency**: soft-currency drops per enemy type, persists, **spend in shop on weapons**, run summary screen
- **Daily login + Achievements**: UTC streak rewards (50→500), 8 achievements (kill/level/win/wave/streak) with retroactive unlock + toast popups
- **Battle Pass**: 10-tier Season 1, XP per kill, free+premium lanes, scrollable claim UI, MainHub tier chip
- **Crash + breadcrumbs**: in-process unhandled-exception capture, 50-entry ring buffer, atomic persistence; pluggable Sentry/Crashlytics adapter slot
- **Front-end**: Main Hub → Play / Loadout / Shop / Settings / Quit (full pre-run flow)
- **Loadout**: pick equipped weapon from unlocked catalog, see equipped perks, Start Run
- **Shop**: Weapons tab (currency spend, Buy/Equip/Locked states reactive) + Cosmetics tab (IAP bundles)
- **Settings**: live audio sliders, sensitivity, vibration/invertY/colorblind toggles, quality, reset-profile
- **HUD**: HP + stamina + dodge pip + wave + combo + score (rolling tween)
- **Audio routing**: SfxRouter bridges all gameplay events to AudioManager.PlaySFX (clips empty — needs SFX asset wiring)
- **Pause**: Esc/Start opens menu, freezes time, Resume/Perks/Restart/Quit
- **Save**: AES JSON atomic with backup rotation + schema versioning
- **DI**: GameLifetimeScope registers 5 services
- **Tests**: 76/76 EditMode pass (+11: CurrencyServiceTests, WeaponShopTests)

## Known issues / TODO
- AudioManager `sounds[]` empty — SFX routes fire but play nothing. Drop in AudioClips next.
- Coplay MCP requires new claude session to attach
- ~~No PlayMode tests yet~~ → ✅ P7.4 shipped GameSceneSmokeTests (2 tests in CI matrix); add more as scenarios accrue
- Run summary score "XP earned" is `score / 10` — derive properly when reward economy is finalized
- Aim Sensitivity slider persists but PlayerController doesn't read it yet (no aim input — strafe is drag-based). Wire when controller/aim added.
- Localization / leaderboards / store cosmetics still untouched
- CrashReporter ships with no-op uploader. Add Sentry Unity SDK (OpenUPM `io.sentry.unity`) + adapter class implementing `ICrashUploader` when ready for symbolicated native crashes
- CI workflows live but won't run until `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` secrets are added to GitHub repo settings
- Battle Pass premium IAP not real yet (uses soft currency for Unlock Premium); wire to IAPManager when premium SKU exists

---

## Test suite

```
Assets/_Game/Tests/EditMode/
├── AchievementServiceTests.cs      (P6.4)
├── BattlePassTests.cs              (P6.5 — NEW: tier-up/claim/premium gating/weapon reward)
├── BossControllerTests.cs
├── ComboTrackerTests.cs            (P2.4)
├── CrashReporterTests.cs           (P7.2)
├── CurrencyServiceTests.cs         (P6.2)
├── DailyLoginServiceTests.cs       (P6.3)
├── DamageSystemTests.cs
├── EnemyBaseTests.cs
├── EventBusTests.cs                (P1.7)
├── ObjectPoolTests.cs
├── PlayerHealthTests.cs
├── PlayerProgressionTests.cs       (P2.8)
├── SaveSystemTests.cs              (P1.2)
├── ScoreCalculatorTests.cs
├── StateMachineTests.cs            (P1.7)
├── TutorialControllerTests.cs      (P4.6)
├── UnlockRegistryTests.cs
├── WaveSpawnerTests.cs
├── WeaponCatalogTests.cs           (P2.11)
└── WeaponShopTests.cs              (P6.2)

Assets/_Game/Tests/PlayMode/
├── GameSceneSmokeTests.cs          (P7.4: scene-boot error capture + WaveSpawner slot reflection)
└── GameplayIntegrationTests.cs     (P7.4 — NEW: BP-XP-on-kill, currency grant, popup spawn, core singletons present)
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
