# Strafe Advance — Session Progress

## Project
- **Path:** `/Users/shishirsingh/Strafe-Advance`
- **GitHub:** https://github.com/shishir48/Strafe-Advance
- **Engine:** Unity 6 (6000.4.7f1), URP, Android target
- **APK:** `/Users/shishirsingh/StrafeAdvance.apk` (last built 2026-05-22, Mono2x)
- **Tests:** 52 passing / 0 failing (EditMode)

---

## Latest Session Summary (2026-05-23)

### Visual / Content
- Player + Grunts use `astronautA.fbx` (Kenney humanoid, scale 2.0) — verified humanoid silhouette
- Flanker uses `craft_speederA.fbx` (scale 1.5), Elite uses `craft_speederC.fbx` (scale 2.0), Boss `turret_double.fbx` (scale 3.0)
- Blaster `blaster-l.fbx` attached to player + grunt right hand (Mesh-local positioning, dynamic material override)
- Muzzle flash particle on every shot (`Assets/Resources/VFX/MuzzleFlash.prefab` via `AutoShooter.SpawnMuzzleFlash`)
- Sci-fi materials: navy + electric blue (player), red (grunts), teal (flankers/elites), gold (boss), all emissive 0.15
- Custom-built corridor tile (primitive floor + walls + glowing edge strips), replaces FBX arch
- URP post-processing: bloom 0.35 threshold 1.2, vignette 0.35, ACES tonemap, cool blue grade
- Camera 3/4 over-shoulder: pos (1.5, 2.5, -3), rot (15, -15, 0)

### Phase 2 — Gameplay Depth (PARTIAL — combat juice batch ✅)

| # | Item | Outcome |
|---|------|---------|
| P2.1 | Damage numbers | Floating TMP, world-space, pooled, color = white normal / gold crit. Subscribes `EnemyDamaged` |
| P2.2 | Screen shake | Perlin-driven trauma model on Main Camera. Hooks `EnemyKilled` (×0.18/0.45), `PlayerDamaged` (×0.55), `ShakeRequest` for custom |
| P2.3 | Hitstop | Time.timeScale freeze 0.04s grunt / 0.10s elite / 0.06s player-hit. `HitstopRequest` event for custom |
| P2.4 | Combo + multiplier | ×1→×2 at 5 kills, ×4 at 10, ×8 at 20. Resets on miss-timeout (2s) or PlayerDamaged. Publishes `ComboChanged`. +5 tests |
| P2.5 | Mixed waves | L1_W4 (Grunt×5 + Flanker×2 @ 2s delay), L1_W7 (Grunt×6 + Flanker×3 @ 1.5s), L1_W9 (Elite×2 + Grunt×4 @ 1s) — uses `WaveEntry[]` |
| P2.6 | Charger enemy | `EnemyType.Charger` added; ChargerEnemy class (lateral homing + melee rusher), config (HP 40, contact 25, speed 6), prefab creation. L1_W6 now mixed (Elite×2 + Charger×3) |
| P2.7 | Power-up dropper | Chance-based drops on enemy kill (Grunt 5%, Flanker 10%, Elite 40%, Charger 10%) via `EventBus<EnemyKilled>`. Cached death pos from EnemyDamaged event |
| P2.8 | XP/level/perks | `PlayerProgression` service: XP per kill (Grunt 10, Flanker 25, Elite 75, Charger 20), quadratic level curve (100×N²), auto-unlock perk on level-up. `Perk` data + `PerkCatalog` (5 perks). SaveData persists level/xp/unlocked/equipped. `GetEquippedStats()` multiplies through equipped perks. +4 tests |
| P2.9 | Dodge roll | Spacebar / gamepad B / 2-finger tap. 0.25s dash @ 4x strafe speed with i-frames via `PlayerHealth.SetInvincible`. 1.5s cooldown. Publishes `DodgePerformed` |
| P2.10 | Sniper enemy | `EnemyType.Sniper` + `SniperEnemy` class: holds position at z=18, telegraphs shots with red LineRenderer for 0.7s, fires homing 25-dmg bullet every 2.5s. L1_W8 now mixed (Flanker×5 + Sniper×2 @ 0.5s delay) |
| P2.11 | Weapon system | `WeaponConfig` + `WeaponCatalog` (5 weapons: Standard Blaster, Rapid SMG, Heavy Cannon, Scatter Gun, Tracker Pistol). AutoShooter reads `SaveData.equippedWeaponId` at start, applies `PlayerProgression.GetEquippedStats()` perks on top. Multishot spread fan + homing strength per weapon. +4 tests |
| P2.12 | Shielded enemy | `EnemyType.Shielded` + `ShieldedEnemy` class: overrides `TakeDamage`, blocks front-cone (70° half-angle) hits, shield breaks after 5 chips. Forces flanking. ShieldHit event for VFX hooks. Front shield child Cube visual with alpha fade |
| P2.13 | Splitter enemy | `EnemyType.Splitter` + `SplitterEnemy` class: on death spawns 3 mini-grunts (60% scale, 40% HP, 1.4× speed) via passed Grunt prefab/config |
| P2.14 | Perk equip UI | `PerkEquipPanel` runtime-built canvas. Auto-opens on `PlayerLeveledUp`. Lists unlocked perks, tap to toggle equip (max 3). Persists via SaveSystem. Calls `AutoShooter.RefreshLoadout()` so mid-run perk change takes effect |

L1_W5 now mixes Flanker+Shielded+Splitter. L1_W8 mixes Flanker+Sniper. L1_W6 mixes Elite+Charger. L1_W4/W7/W9 mixed earlier.

Phase 2 remaining: drone swarm / mini-boss enemies, AI behavior trees, sprint+slide, ragdoll death, Cinemachine kill cam.

### Phase 1 — Foundation Refactor (COMPLETE ✅)

| # | Item | Outcome |
|---|------|---------|
| P1.1 | Fix 4 pre-existing EditMode test failures | 36/36 green |
| P1.2 | SaveSystem — AES-256-CBC + JSON + atomic write + backup + schema versioning | +4 tests, `Assets/_Game/Scripts/Core/SaveSystem.cs` |
| P1.3 | Mixed-type `WaveConfig.entries[]` (parallel spawn coroutines, start delays) | +2 tests, back-compat with legacy single-type fields |
| P1.4 | New Input System migration — `GameInput` facade, all `UnityEngine.Input` gone | `Assets/_Game/Scripts/Core/GameInput.cs` |
| P1.5 | Addressables — `AssetLoader` facade (Addressables → Resources fallback), 9 keys registered | menu item `StrafAdvance/11. Bootstrap Addressables` |
| P1.6 | VContainer DI — `GameLifetimeScope` with 5 services registered, ready for migration of `FindAnyObjectByType` callsites | `Assets/_Game/Scripts/Core/GameLifetimeScope.cs` |
| P1.7 | `EventBus<T>` typed pub/sub + generic `StateMachine<TState>` with validated transitions; `GameManager` FSM-driven | +9 tests |

### Other bug fixes this session
- Enemy layer was missing (id=-1) — re-added via `StrafAdvance/1. Add Enemy Layer`
- `runInBackground = true` in ProjectSettings (was 0, broke MCP-driven testing)
- Singletons (GameManager/IAPManager/AudioManager) now `[RuntimeInitializeOnLoadMethod] static void ResetStatics()` for domain-reload safety
- WaveSpawner: `EnemyBase.Die()` uses `DestroyImmediate` in EditMode for test safety
- WaveSpawner: auto-advance gated on `Application.isPlaying` (prevents test coroutine crashes)
- SwapMesh: strips root `MeshFilter`+`MeshRenderer`, zeroes Mesh `localPosition` (FBX pivot fix)
- Level configs (`Resources/Level1-3.asset`) synced from SOPath whenever `CreateScriptableObjects` runs

---

## Open Roadmap

See `docs/ROADMAP.md` — Phase 2 onward (gameplay depth, visual production, UI/UX, audio, monetization, ship).

---

## What Works ✅

- 10 waves spawn and advance through Level 1/2/3
- Enemies die when shot; wave never stalls (EscapeOffScreen counts as kill)
- Tap-to-start screen (device: tap, editor: 3s auto-start)
- GAME OVER overlay on player death, YOU WIN overlay on boss kill
- HUD: electric blue HP bar + wave label (self-wiring `HUDController.AutoWire()`)
- HumanoidA + B Kenney models with Blaster + muzzle flash
- Save system ready (not yet wired to gameplay; needs Phase 2 progression)
- DI container ready (services registered, callsites still use `Find`/static singletons)
- StateMachine + EventBus ready (legacy `OnStateChanged` still fires alongside `EventBus<GameStateChanged>`)

---

## Known Issues / TODO

- `[Inject]` migration of legacy `FindAnyObjectByType` callsites (use container instead)
- `WaveSpawner.OnWaveStarted` event still legacy `Action<int>` — should publish `EventBus<WaveStarted>`
- Tests are 52 EditMode only — no PlayMode tests yet (Phase 7 ship-quality work)
- Coplay MCP installed but only works in NEW terminal session (`claude` restart)
- IAPManager initialization warning ("Unity Gaming Services not initialized") — harmless, deal with in Phase 6 monetization
- HUD wave-label loads `Level1` via `AssetLoader.Load`, hardcoded — should pull from active LevelConfig via DI

---

## Key Files

| File | Purpose |
|------|---------|
| `Assets/_Game/Scripts/Core/GameManager.cs` | FSM-driven game state + auto-start coroutine |
| `Assets/_Game/Scripts/Core/SaveSystem.cs` | AES JSON atomic save |
| `Assets/_Game/Scripts/Core/EventBus.cs` | Typed pub/sub |
| `Assets/_Game/Scripts/Core/StateMachine.cs` | Generic FSM |
| `Assets/_Game/Scripts/Core/AssetLoader.cs` | Addressables → Resources fallback |
| `Assets/_Game/Scripts/Core/GameLifetimeScope.cs` | VContainer DI scope |
| `Assets/_Game/Scripts/Core/GameInput.cs` | New InputSystem facade |
| `Assets/_Game/Scripts/Level/WaveSpawner.cs` | Wave logic + kill tracking + mixed-entry support |
| `Assets/_Game/Scripts/Level/WaveConfig.cs` | `WaveEntry[]` mixed-type waves |
| `Assets/_Game/Scripts/Editor/GameSetup.cs` | All setup menu items |
| `Assets/_Game/Scripts/Editor/AddressablesSetup.cs` | One-click Addressables bootstrap |
| `Assets/Resources/Level1.asset` | Level config loaded at runtime |
| `Assets/Resources/Boss.prefab` | Boss prefab loaded at runtime |
| `Assets/Resources/VFX/*.prefab` | HitSpark, EnemyDeath, BossDeath, MuzzleFlash |

---

## StrafAdvance Menu Items

| Item | Action |
|------|--------|
| Play Game | Toggle play mode |
| 1. Add Enemy Layer | Add Enemy tag+layer to TagManager |
| 2. Create ScriptableObject Assets | Regenerate enemy/wave/level configs (10 waves × 3 levels) |
| 3. Create Prefabs | Initial prefab scaffolding |
| 4. Setup GameScene | Re-create GameScene hierarchy (incl. GameLifetimeScope) |
| 5. Setup Bootstrap Scene | Build settings + bootstrap scene |
| 6. Create Materials & Apply to Prefabs | Basic material assignment |
| 7. Wire HUD in Scene | HUD sliders/labels |
| 8. Upgrade Graphics (Sci-Fi Neon) | Legacy neon style (superseded by 10) |
| 9. Apply Kenney 3D Models | Swap primitives for FBX characters + attach blaster |
| 10. Apply Sci-Fi Upgrade | Materials + post-processing + VFX + corridor rebuild + bullet trail |
| 11. Bootstrap Addressables | Register everything under `Assets/Resources` as Addressables |
| Build Android APK | Build Mono2x APK via BuildPipeline |
| Rewire Player Prefab | Re-assign config/firePoint refs |

---

## Test Suite

```
Assets/_Game/Tests/EditMode/
├── BossControllerTests.cs
├── DamageSystemTests.cs
├── EnemyBaseTests.cs
├── EventBusTests.cs        (new — Phase 1)
├── ObjectPoolTests.cs
├── PlayerHealthTests.cs
├── SaveSystemTests.cs      (new — Phase 1)
├── ScoreCalculatorTests.cs
├── StateMachineTests.cs    (new — Phase 1)
├── UnlockRegistryTests.cs
└── WaveSpawnerTests.cs
```

Run via `mcp__mcp-for-unity__run_tests` or Unity Test Runner window.
