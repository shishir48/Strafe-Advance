# Strafe Advance — Hybrid-Casual Gameplay Overhaul

**Date:** 2026-05-30
**Branch:** `feat/hybrid-casual-gameplay` (independent of `feat/phase3-industrial-visuals`)
**Status:** Design — approved shape, pending spec review

## Goal

Add the gameplay + gamification layer that makes Strafe Advance a **hybrid-casual** game: an accessible hyper-casual on-ramp and short "one more run" loop, sitting on top of the light meta-depth already built (perks, weapons, battle pass, currency, daily login, achievements). Drive retention + LTV without stripping the existing systems and without adding control complexity.

Success = a new player can fall into an endless score-chase loop, die, and immediately want to go again — with reward dopamine, run variety, and a revive/continue hook pulling them back.

## Positioning (locked)

**Hybrid-casual**, NOT a full hyper-casual pivot. Keep the premium systems as *optional* depth; make the surface loop instant and addictive. Monetization stays compatible with both the existing $4.99/IAP roadmap and future free+ads.

## Locked Scope (4 features)

1. Endless Arcade Mode (the core loop / engine)
2. More enemies + mid-run surge events
3. Reward juice + milestones
4. Revive + one-more-run hooks

## Ground Truth (verified in code)

- `GameState` enum: `Menu, Playing, BossFight, LevelComplete, GameOver`. Strict FSM in `GameManager`; `GameOver` only transitions → `Menu`.
- `WaveSpawner.SpawnWave(index)` reads fixed `_level.waves[index]`, fires `OnAllWavesComplete` at `waves.Length`. Spawn types via a `switch (EnemyType)` with per-type prefab/config fields. 8 enemy types wired today.
- `DifficultyService` is `static`, returns a multiplier keyed only to `SaveSystem.Current.progress.playerLevel` (×1→×3). No run-local escalation.
- `GameManager.BeginRunFromMenu()` loads `Level1`, wires `WaveSpawner` + `CorridorScroller` + boss-on-complete. `MainHubController` drives the Play button.
- `IAPManager` = Unity IAP (NonConsumable level packs + skins, Consumable `powerup_pack`). **No ads SDK installed.**
- Existing: `CurrencyService` (spend/earn + `CurrencyEarned`), `ComboTracker`, `ScreenShake`/`Hitstop`/`KillCam`, `ToastNotifier`, `ScorePopupSpawner`, `CurrencyPopupSpawner`, `SfxRouter`, `EventBus<T>`, `LowHpVignette`, `NearDeathEffect`. Reuse these — do not duplicate.
- Test suites: 105 EditMode + 6 PlayMode green. Services are unit-tested; new services follow the same pattern.

## Non-Goals

- No control-scheme change (drag-to-strafe + auto-shoot stays — it's already casual).
- No ad-SDK integration (revive uses currency now; ad path is a stubbed interface).
- No removal/dumbing-down of perks/weapons/battle pass.
- No visual-art work (that's the other spec).

## Architecture / Approach

Four features as payoff-ordered stages on `feat/hybrid-casual-gameplay`. Each is an isolated unit with its own EditMode tests, MCP play-mode verification, and its own commit. Endless mode is built first because features 2–4 plug into its director.

### Feature 1 — Endless Arcade Mode (the engine)
The core hyper-casual loop, added *alongside* the existing fixed-level mode.

- **Wave-source abstraction:** extract an `IWaveProvider` (or `GetNextWave()` seam) from `WaveSpawner`. Two impls: `FixedLevelProvider` (wraps current `LevelConfig.waves`) and `EndlessProvider` (procedurally synthesizes `WaveConfig`s with escalating count / type-mix / interval). `WaveSpawner` asks the provider for the next wave instead of indexing a fixed array; existing level mode keeps identical behavior.
- **Run-local difficulty:** `RunDifficulty` value that escalates with wave index / elapsed time, layered on top of static `DifficultyService` (multiplied, not replacing meta-scaling).
- **Run mode flag:** `GameManager` gains a `RunMode { Campaign, Endless }`; `BeginEndlessRun()` mirrors `BeginRunFromMenu()` but uses `EndlessProvider` and never auto-ends. Periodic boss/mini-boss every N waves instead of a single end-boss.
- **Best score:** persist `bestEndlessScore` in `SaveData`; surface on run-end + MainHub.
- **Entry point:** "ARCADE" button in `MainHubController` → `BeginEndlessRun()`.
- **Tests:** `EndlessProvider` escalation monotonicity, `RunDifficulty` curve + cap, best-score persistence.
- **Verify:** MCP play-mode — arcade run streams forever, difficulty visibly ramps, death → run summary with best score.

### Feature 2 — More enemies + mid-run surge events
Variety + spikes the endless loop needs, no new controls.

- **2–3 new enemy types** following the `EnemyBase` + `WaveSpawner` switch pattern. Candidates: **Bomber** (AoE burst on death — telegraphed), **Healer** (periodic heal aura to nearby enemies — priority target), **Turret/Spawner** (stationary, drip-spawns or suppresses). Each: prefab + `EnemyConfig` + spawner case + `EnemyType` enum entry + score reward. Reuse existing telegraph/hit-react juice.
- **Surge events:** `EndlessDirector` fires scripted spikes at milestones — `SwarmRush` (drone flood), `EliteAmbush`, `Gauntlet`. New `EventBus<SurgeEvent>` → `ToastNotifier`/HUD banner announces ("⚠ SWARM INCOMING").
- **Tests:** new enemy death/score/escape wiring (mirror `EnemyBaseTests`); surge scheduling cadence.
- **Verify:** MCP play-mode — new enemies behave + score correctly; a surge fires with banner; no console errors.

### Feature 3 — Reward juice + milestones
The dopamine layer — pure feel, reuses existing infra.

- **`MilestoneService`** subscribes to `EnemyKilled` / `ComboChanged` / score / wave-advance and fires celebrations at thresholds: score milestones (every N), kill-streaks, combo tiers, wave milestones, new-best.
- **Presentation:** coin-fountain burst (reuse `CurrencyPopupSpawner`), level-up/milestone burst, streak banner (reuse `ToastNotifier`), escalating chime (existing `SfxRouter`/`AudioManager`), screen punch (existing `ScreenShake`/`Hitstop`). New `EventBus<MilestoneReached>` so visuals/audio subscribe independently.
- **Tests:** threshold-firing logic (fires once per threshold, escalates, resets per run).
- **Verify:** MCP play-mode — hitting a milestone triggers fountain + banner + chime; no double-fire.

### Feature 4 — Revive + one-more-run hooks
Retention + monetization loop.

- **FSM:** add `fsm.Allow(GameState.GameOver, GameState.Playing)` for revive resume.
- **`ReviveService`:** on death, offer revive — clears/pushes back nearby enemies, brief invulnerability, resumes run. Cost = `CurrencyService` spend, **escalating per revive within a run** (e.g. 100 → 250 → 500). Pluggable `IRewardedAd` interface (stub `NoOpRewardedAd` now, same pattern as `ICrashUploader`) so a real ad-SDK revive drops in later.
- **Run-end hooks:** 2x-coin offer (currency-confirm now, ad-stub path), comeback-streak counter, near-miss feedback (reuse `NearDeathEffect`/`LowHpVignette`).
- **UI:** revive prompt on the existing `RunSummaryPanel` / GameOver flow (timed countdown → auto-decline, classic HC pattern).
- **Tests:** revive cost escalation, currency gating (can't revive without funds), FSM transition legality, streak counter.
- **Verify:** MCP play-mode — die, revive with currency, resume mid-run; second revive costs more; decline → run summary.

## Risks

1. **WaveSpawner seam** — extracting the provider must not regress the 8-type spawn logic or the wave-advance/dead-lock guards (`_enemiesAlive`, `_spawning`). Mitigation: keep `FixedLevelProvider` behavior byte-for-byte equal to today; cover with existing `WaveSpawnerTests` + new ones before swapping.
2. **FSM revive transition** — opening `GameOver→Playing` must not let illegal flows through elsewhere. Mitigation: single explicit allow + test that other transitions still reject.
3. **Endless balance** — runaway or trivial difficulty. Mitigation: tune `RunDifficulty` curve with a cap; verify via play-mode at several wave depths.
4. **Revive without ads** — currency-only revive may feel pay-walled with soft currency. Mitigation: first revive cheap/affordable; ad-stub clearly the intended free path later.
5. **Juice overload** — milestone spam competing with combat readability. Mitigation: throttle/queue via `ToastNotifier`; thresholds spaced.

## Verification Strategy

Per feature: new EditMode tests (service logic) + MCP play-mode screenshot/console check. Existing 105 EditMode + 6 PlayMode must stay green as a regression gate (run via `mcp-for-unity run_tests`). Commit only on green + visibly-working result. Only own files staged per commit (sprint-7 WIP in tree untouched).

## Relationship to Other Work

- Independent of `feat/phase3-industrial-visuals` (visual overhaul). Both branch off `sprint-7-stability`; merge order doesn't matter (no shared files expected beyond possibly `SaveData`/`MainHubController`, which will be coordinated at merge).
- Builds directly on roadmap Phase 2 (gameplay depth) and Phase 6 (monetization) without contradicting them.

## Out-of-Scope Follow-ups (noted, not now)

- Real rewarded-ad SDK (IronSource/AppLovin) behind `IRewardedAd`.
- Leaderboards for endless best-score (roadmap Phase 6 — Lootlocker).
- Daily challenges / run quests (could be a 3rd gamification spec).
